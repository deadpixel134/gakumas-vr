using System.Numerics;
using System.Runtime.InteropServices;

namespace Doorstop;

internal sealed partial class MainThreadSampler
{
    private enum ContactChannel { Rotation, Scale }
    private readonly record struct ContactChannelLease(ContactTrialTarget Target, ContactChannel Channel, Vector4 Before, Vector4 Written);
    private readonly List<ContactChannelLease> _contactChannels = new(MaxContactTrialTargets * 2);
    private IntPtr _contactReadRotation, _contactWriteRotation, _contactReadScale, _contactWriteScale, _contactQuaternion;

    private void ResolveContactDeformation(IntPtr coreImage, IntPtr transform)
    {
        try
        {
            _contactQuaternion = FindClass(coreImage, "UnityEngine", "Quaternion");
            _contactReadRotation = FindMethod(transform, "get_localRotation");
            _contactWriteRotation = FindMethodBySignature(transform, "set_localRotation", "UnityEngine.Quaternion");
            _contactReadScale = FindMethod(transform, "get_localScale");
            _contactWriteScale = FindMethodBySignature(transform, "set_localScale", "UnityEngine.Vector3");
            _contactBoneToWorld = FindMethod(transform, "get_localToWorldMatrix");
        }
        catch (Exception error)
        {
            _contactReadRotation = _contactReadScale = IntPtr.Zero;
            LogContactPilot(DateTimeOffset.UtcNow, "contact-extra-channels-unavailable", error.GetType().Name);
        }
    }

    private bool ApplyContactChannels(ContactTrialTarget target)
    {
        bool wrote = false;
        if (_contactReadRotation != IntPtr.Zero && target.PairTilt != Vector3.Zero)
        {
            Vector4 before = ReadContactChannel(target, ContactChannel.Rotation);
            float angle = target.PairTilt.Length();
            var baseline = new Quaternion(before.X, before.Y, before.Z, before.W);
            if (float.IsFinite(baseline.LengthSquared()) && baseline.LengthSquared() > .5f)
            {
                Quaternion rotation = Quaternion.Normalize(baseline * Quaternion.CreateFromAxisAngle(target.PairTilt / angle, angle));
                wrote |= StageContactChannel(target, ContactChannel.Rotation, before, new Vector4(rotation.X, rotation.Y, rotation.Z, rotation.W));
            }
        }
        if (_contactReadScale != IntPtr.Zero && target.PairScale != Vector3.One)
        {
            Vector4 before = ReadContactChannel(target, ContactChannel.Scale);
            if (float.IsFinite(before.LengthSquared()) && before.X != 0 && before.Y != 0 && before.Z != 0)
                wrote |= StageContactChannel(target, ContactChannel.Scale, before,
                    before * new Vector4(target.PairScale, 1));
        }
        return wrote;
    }

    private bool StageContactChannel(ContactTrialTarget target, ContactChannel channel, Vector4 before, Vector4 written)
    {
        if (before == written) return false;
        int index = _contactChannels.Count;
        _contactChannels.Add(new(target, channel, before, written));
        try
        {
            WriteContactChannel(target, channel, written);
        }
        catch
        {
            // A setter can fail after committing a normalized quaternion. Capture that value
            // before unwinding so the same exact-ownership rule can restore it.
            Vector4 partial = ReadContactChannel(target, channel);
            if (partial != before && ContactChannelMatchesWrite(channel, partial, written))
            {
                _contactChannels[index] = new(target, channel, before, partial);
                _contactTrialWrites++;
            }
            throw;
        }
        Vector4 actual = ReadContactChannel(target, channel);
        if (!ContactChannelMatchesWrite(channel, actual, written))
            throw new InvalidOperationException("Contact transform write did not round trip.");
        // Unity may normalize a quaternion. The exact readback is the owned value.
        _contactChannels[index] = new(target, channel, before, actual);
        _contactTrialWrites++;
        return true;
    }

    private bool RestoreContactChannel(ContactChannelLease lease)
    {
        try
        {
            if (!ContactTrialAlive(lease.Target.Bone) || Invoke(_contactTrialParent, lease.Target.Bone) != lease.Target.Parent)
            {
                FailContactBoneTrial("bone-or-parent-changed-during-render");
                return true;
            }
            Vector4 current = ReadContactChannel(lease.Target, lease.Channel);
            if (current == lease.Written)
            {
                WriteContactChannel(lease.Target, lease.Channel, lease.Before);
                if (!ContactChannelMatchesWrite(lease.Channel, ReadContactChannel(lease.Target, lease.Channel), lease.Before))
                    throw new InvalidOperationException("Contact transform restore did not round trip.");
                _contactTrialRestores++;
            }
            else if (current != lease.Before)
            {
                _contactTrialExternalChanges++;
                FailContactBoneTrial("animation-replaced-contact-channel");
            }
            return true;
        }
        catch (Exception error)
        {
            FailContactBoneTrial("restore-contact-channel", error);
            return false;
        }
    }

    private static bool ContactChannelMatchesWrite(ContactChannel channel, Vector4 actual, Vector4 expected) =>
        actual == expected || (channel == ContactChannel.Rotation &&
            MathF.Min(Vector4.DistanceSquared(actual, expected), Vector4.DistanceSquared(actual, -expected)) <= 1e-10f);

    private Vector4 ReadContactChannel(ContactTrialTarget target, ContactChannel channel)
    {
        if (channel == ContactChannel.Scale) return new(ReadContactTrialVector(Invoke(_contactReadScale, target.Bone)), 0);
        IntPtr boxed = Invoke(_contactReadRotation, target.Bone);
        IntPtr value = boxed == IntPtr.Zero ? IntPtr.Zero : _api.ObjectUnbox(boxed);
        return value == IntPtr.Zero ? throw new InvalidOperationException("Missing contact rotation.") : Marshal.PtrToStructure<Vector4>(value);
    }

    private unsafe void WriteContactChannel(ContactTrialTarget target, ContactChannel channel, Vector4 value)
    {
        if (channel == ContactChannel.Scale)
            _ = InvokeWithObjectArgument(_contactWriteScale, target.Bone, BoxContactTrialVector(new(value.X, value.Y, value.Z)));
        else
            _ = InvokeWithObjectArgument(_contactWriteRotation, target.Bone, _api.ValueBox(_contactQuaternion, (IntPtr)(&value)));
    }
}
