[한국어](../ko/VR_INTERACTION_SPEC.md) | [English](../en/VR_INTERACTION_SPEC.md) | [日本語](VR_INTERACTION_SPEC.md)

# 再利用可能なVR操作・ポーズ合成仕様

[プロジェクトホーム](../../README.ja.md) · [使用方法](USAGE.md) · [構造](ARCHITECTURE.md)

この文書は、Gakumas VR v0.173.0でユーザー実機確認され、v0.174.0でデフォルト値を変更した操作感と安全構造を、別のUnityゲームのVR Modでも再現するための実装契約です。ゲーム固有のカメラ検出は変更できますが、以下の座標・入力・表示の不変条件を維持します。

## ユーザー向け動作

- デフォルトは左スティックが視点回転、右スティックが移動です。設定で移動側を変更すると両方の役割が入れ替わります。
- 移動は最終視点の完全な3D方向に従います。上を向いて前進すると上昇し、下を向くと下降します。
- スティック回転はワールドyawまたはpitchだけを変更し、rollを生成しません。
- デフォルトは30°スナップです。15°/30°/45°/60°とスムーズ回転を選べます。
- 独立ライブ6DoFはデフォルトONで、移動速度のデフォルトは1.95m/sです。
- VR原点設定後にHMDを実際に傾けたroll差分だけを保持します。開始時の傾き、シーンカメラのroll、スティック由来のrollは除去します。
- VR内ではスティックスクロールを使いません。
- 新しいステレオがなければ最終ゲーム画面を正面パネルに表示します。3Dでは同じ画面を手元パネルで表示し、反対側のrayとボタンで操作します。
- VR失敗時もデスクトップゲームを継続し、パネルまたはVR無効へフォールバックします。

## 座標と原点

OpenXRからUnityへの変換は次の通りです。

```text
positionUnity = ( x,  y, -z)
rotationUnity = (-x, -y,  z, w)
```

ステレオgeneration開始時に両目の中央位置と正規化した平均orientationを原点として一度保存します。相対位置は `inverse(origin) × (current - origin)` で求めます。generation破棄時はpose mapper、移動offset、人工回転、入力latchを同時に初期化します。

ライブ独立6DoFは入場時のゲームカメラのワールドposeを一度anchorとして保存し、その後の演出カメラ経路からVR視点を独立させます。非ライブ3Dでは検証済みの現在source cameraを基準にします。

## 移動

deadzone処理した `(strafe, 0, forward)` を最終視点quaternionで回転します。XZ平面へ投影してはいけません。積分 `dt` は最大0.1秒、デフォルトdeadzoneは0.20、速度は1.95m/sです。物理頭部移動、眼offset、スティック移動は同一のワールドnavigation basisを使います。

## スティック回転

各サンプルで優勢な一軸だけを選びます。`abs(x) >= abs(y)` ならyawのみ、それ以外はpitchのみです。物理スティックの斜め誤差による意図しない斜め回転を防ぎます。

スナップは0.65で発火し、設定角度を一度だけ適用します。スティックが0.20 deadzone内へ戻るまで再発火しません。スムーズ回転は選択軸を設定速度で積分し、`dt <= 0.1`、人工pitchは約±89.1°に制限します。

人工yaw/pitchはscalarで保持し、毎フレームquaternionを再構築します。前フレームの最終quaternionへ増分を繰り返し乗算すると軸driftとroll混入が起きるため使用しません。

## roll分離と最終回転

各HMD eye orientationをUnity座標に変換し、次のように分解します。

```text
forward = rotation × (0,0,1)
right   = rotation × (1,0,0)
up      = rotation × (0,1,0)

yaw   = atan2(forward.x, forward.z)
pitch = atan2(-forward.y, length(forward.xz))
roll  = atan2(right.y, up.y)
```

原点yaw/pitch/rollを別々に保存し、現在値との差分を取ります。yaw/roll差分は `[-π, π]` にwrapします。シーンカメラはforwardからbase yaw/pitchだけを取得し、base rollを破棄します。

```text
finalYaw   = baseYaw   + artificialYaw   + physicalYawDelta
finalPitch = clamp(basePitch + artificialPitch + physicalPitchDelta)
finalRoll  = physicalRollDelta

finalRotation = Yaw(finalYaw) × Pitch(finalPitch) × Roll(finalRoll)
```

このためスティックはrollを変更できません。開始時のHMD傾きは原点で相殺され、同じ傾きを保ったまま首を左右に回しても新しいrollは発生せず、開始後に実際に追加した首の傾きだけが残ります。

相対quaternion全体を `artificial × inverse(origin) × current` として適用してはいけません。傾いた原点では物理yawの一部が相対rollになり、スティック回転後に水平線が傾くことがあります。

## パネルと入力

- fresh stereoなし: view-space正面quadが主コンテンツです。
- fresh stereoあり: projection worldが主で、手元パネルは補助UIです。
- パネル中心はcontroller tip、デフォルトはview-spaceで垂直・viewer-facingです。tracking/FOV外では非表示にし、copy/acquire/submit/hit-testも停止します。
- rayとpanel planeの交点をUVへ変換し、letterbox領域を除外してゲームclient座標へ変換します。
- デフォルトA/Triggerはclick/drag、Bは戻るです。Trigger初期位置を先にlatchして、引く動作による照準ずれを抑えます。
- ゲームがforegroundでない場合はWindows入力を注入せず、遷移時には保持中ボタンを必ずreleaseします。

## レンダリング・寿命・フォールバック

- 左右clone cameraはUnityの通常レンダーループで描画し、両目が完成したpairだけを公開します。
- ゲーム描画周期とOpenXR submit周期を分離し、最新の完成pairをHMD周期で再提出できます。
- camera、eye texture、render request、GPU queryはscene-bound generation資源として扱い、シーン破棄後の古いUnity wrapperを再利用しません。
- 全オブジェクト列挙は低周期・変更駆動診断に限定し、1〜10msの遷移fast pathでは行いません。
- source/clone/OpenXR失敗時はprojectionを停止し、最終backbuffer panelへフォールバックします。ゲーム本体DLLやassetは変更しません。

## 他ゲームへの移植境界

再利用層はOpenXR session/action/swapchain、pose分解、移動・回転integrator、panelとpointer mapping、generation寿命、安全fallback、設定検証、manifest install、rollbackです。

ゲーム固有adapterは実world camera判定、render pipelineのclone方式、合成済みbackbuffer取得、方向・scene transition、VFX overrideを実装します。scene名やcamera名だけで3Dを許可せず、実際のrender targetと表示surfaceの関係を確認します。

## 必須回帰テスト

- 傾いたscene cameraでsnap yawを繰り返してもworld-rightのYがほぼ0か。
- 傾いたHMD原点で傾きを保ったphysical yawがroll delta 0を作るか。
- HMDを実際に15°追加で傾けた場合だけ最終rollが約15°になるか。
- snap保持中は一度だけ発火し、中央復帰後に再発火するか。
- 斜めスティックノイズで優勢軸だけが変わるか。
- 上下を見ながら前進して上昇・下降するか。
- 移動側変更時に移動・回転sourceが共に交換されるか。
- 3D退出時に古い3Dフレームを残さず正面パネルが復帰するか。
- install/update/uninstallがユーザー設定と他Modファイルを保護するか。

自動テストは数式とファイル安全契約を検証します。HMD runtime、ゲームカメラ統合、体感はユーザーVR実機で別途判定します。
