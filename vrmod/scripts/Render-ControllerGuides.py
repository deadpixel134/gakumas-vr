"""Lay out translated bindings around Meta's official Quest 3 Touch Plus PNG.

No controller pixels are redrawn. Source and attribution are in docs/images/
meta-quest-touch-plus. Pillow and Windows CJK fonts are required.
"""
from pathlib import Path
import hashlib
from PIL import Image, ImageDraw, ImageFont

ROOT = Path(__file__).resolve().parents[2]
IMAGES = ROOT / "docs" / "images"
SOURCE = IMAGES / "meta-quest-touch-plus" / "Quest_03.png"
EXPECTED_HASH = "d7286a92abb9726223f20ad6f5ba8eae1e1a17681fbd77c01f857bfacccd3b33"
if hashlib.sha256(SOURCE.read_bytes()).hexdigest() != EXPECTED_HASH:
    raise ValueError("Official source PNG differs from the recorded Meta asset")

COPY = {
    "ko": {
        "title": "VR 컨트롤 안내", "subtitle": "Meta Quest 3 · Touch Plus 공식 이미지 · 기본 조작",
        "left": "왼손", "right": "오른손", "stick": "스틱", "grip": "Grip",
        "fingers": "Trigger = 앞쪽 검지 버튼    |    Grip = 옆쪽 중지 버튼",
        "left_rows": [
            ("스틱 기울이기", "시야 회전", "좌우·상하 회전 · 기본 30° 스냅"),
            ("스틱 클릭", "시점 재정렬", "게임 카메라 기준으로 시점·누적 이동 복귀"),
            ("Grip 짧게", "보조 패널 전환", "3D 장면에서 짧게 눌렀다 놓기"),
            ("Grip 약 0.8초", "VR 설정 열기 / 닫기", "헤드셋 안에서 설정 · 짧은 누르기와 구분"),
        ],
        "right_rows": [
            ("스틱 기울이기", "3D 이동", "바라보는 방향으로 이동 · 상하 포함"),
            ("스틱 클릭", "카메라 추종 전환", "게임 카메라 추종 ↔ 고정 시점"),
            ("Grip + 손 이동", "월드 그립", "손과 반대 방향으로 시점 이동 · 놓으면 유지"),
            ("Trigger / A / B", "패널 조작", "레이 조준 · Trigger 또는 A 선택 · B 뒤로"),
        ],
        "note_title": "상하·미세 이동은 오른손 월드 그립을 권장합니다.",
        "note": "스틱·월드 그립은 6DoF 장면에서 사용합니다. 게임 패널 입력은 기본적으로 게임 창 포커스가 필요합니다.",
        "consent": "캐릭터 접촉·흔들림·햅틱: 설정 GUI에서 최초 안내문을 끝까지 읽고 1회 동의한 뒤 사용합니다.",
    },
    "en": {
        "title": "VR controls", "subtitle": "Meta Quest 3 · Official Touch Plus artwork · Default bindings",
        "left": "Left hand", "right": "Right hand", "stick": "Stick", "grip": "Grip",
        "fingers": "Trigger = index finger, front    |    Grip = middle finger, side",
        "left_rows": [
            ("Tilt stick", "Turn the view", "Yaw and pitch · 30° snap by default"),
            ("Click stick", "Recenter", "Reset the view and movement to the game camera"),
            ("Tap Grip", "Toggle auxiliary panel", "Press and release quickly in a 3D scene"),
            ("Hold Grip ~0.8 s", "Open / close VR settings", "Change settings inside the headset"),
        ],
        "right_rows": [
            ("Tilt stick", "Move in 3D", "Follow your view, including up and down"),
            ("Click stick", "Toggle camera follow", "Follow the game camera ↔ fixed viewpoint"),
            ("Grip + move hand", "World Grip", "View moves opposite your hand; release to stay"),
            ("Trigger / A / B", "Panel controls", "Aim ray · Trigger or A selects · B goes back"),
        ],
        "note_title": "Use right-hand World Grip for vertical movement and fine positioning.",
        "note": "Stick navigation and World Grip use 6DoF scenes. Game-panel input requires the game window to be focused by default.",
        "consent": "Character contact, jiggle and haptics require one-time consent after reading the full notice in the configurator.",
    },
    "ja": {
        "title": "VR操作ガイド", "subtitle": "Meta Quest 3 · Touch Plus公式画像 · デフォルト操作",
        "left": "左手", "right": "右手", "stick": "スティック", "grip": "Grip",
        "fingers": "Trigger = 前側の人差し指ボタン    |    Grip = 側面の中指ボタン",
        "left_rows": [
            ("スティックを倒す", "視点回転", "左右・上下に回転 · 既定は30°スナップ"),
            ("スティック押し込み", "視点リセンター", "ゲームカメラを基準に視点と移動を戻す"),
            ("Grip短押し", "補助パネル切り替え", "3Dシーンで短く押して離す"),
            ("Grip約0.8秒", "VR設定を開く / 閉じる", "ヘッドセット内で設定 · 短押しと区別"),
        ],
        "right_rows": [
            ("スティックを倒す", "3D移動", "視線に沿って移動 · 上下にも対応"),
            ("スティック押し込み", "カメラ追従切り替え", "ゲームカメラ追従 ↔ 固定視点"),
            ("Grip + 手を動かす", "ワールドグリップ", "手と逆方向に視点移動 · 離すと位置を維持"),
            ("Trigger / A / B", "パネル操作", "レイで狙う · TriggerかAで選択 · Bで戻る"),
        ],
        "note_title": "上下移動や微調整には右手のワールドグリップがおすすめです。",
        "note": "移動と視点操作は6DoFで使用します。ゲームパネル入力は既定でゲームウィンドウのフォーカスが必要です。",
        "consent": "キャラクター接触・揺れ・ハプティクスは、設定GUIの初回案内を最後まで読み同意してから使用します。",
    },
}


def font(lang, size, bold=False):
    if lang == "ja":
        path = "C:/Windows/Fonts/YuGothB.ttc" if bold else "C:/Windows/Fonts/YuGothR.ttc"
    elif lang == "ko":
        path = "C:/Windows/Fonts/malgunbd.ttf" if bold else "C:/Windows/Fonts/malgun.ttf"
    else:
        path = "C:/Windows/Fonts/segoeuib.ttf" if bold else "C:/Windows/Fonts/segoeui.ttf"
    return ImageFont.truetype(path, size)


def fit(draw, lang, text, size, width, bold=False):
    while size > 19:
        f = font(lang, size, bold)
        if draw.textlength(text, font=f) <= width:
            return f
        size -= 1
    raise ValueError(f"Label does not fit: {text}")


def render(lang, words):
    image = Image.new("RGB", (1800, 1620), "#f2f5fa")
    d = ImageDraw.Draw(image)
    ink, muted, border = "#10243a", "#455b72", "#d4deea"
    d.text((60, 34), "GAKUMAS VR", font=font("en", 24, True), fill="#28607e")
    d.text((60, 82), words["title"], font=font(lang, 60, True), fill=ink)
    d.text((62, 163), words["subtitle"], font=font(lang, 28), fill=muted)
    d.rounded_rectangle((1500, 44, 1740, 98), 27, fill="#e2eaf4")
    d.text((1552, 51), "v0.178.4", font=font("en", 28, True), fill=ink)
    original = Image.open(SOURCE).convert("RGB")
    for index, (key, x, color) in enumerate((("left", 60, "#126f79"), ("right", 940, "#66509e"))):
        d.rounded_rectangle((x, 223, x + 800, 1314), 24, fill="white", outline=border, width=2)
        d.rounded_rectangle((x + 28, 245, x + 228, 299), 14, fill=color)
        d.text((x + 52, 250), words[key], font=font(lang, 30, True), fill="white")
        # The source contains both controllers; preserve each original drawing and aspect ratio.
        crop = original.crop((0, 0, 340, 448) if index == 0 else (380, 0, 711, 448))
        scale = 1.0
        crop = crop.resize((round(crop.width * scale), round(crop.height * scale)), Image.Resampling.LANCZOS)
        ix, iy = x + 230, 315
        image.paste(crop, (ix, iy))
        # Callouts only point to visible controls. The front trigger is explained below the artwork.
        stick = (ix + (168 if index == 0 else 164), iy + 40)
        grip = (ix + (152 if index == 0 else 180), iy + 244)
        for name, point, tx, ty in ((words["stick"], stick, x + 40, 361), (words["grip"], grip, x + 620, 570)):
            line_end = (tx + 110, ty + 18) if tx < point[0] else (tx - 12, ty + 18)
            d.line((point, line_end), fill=color, width=3)
            d.ellipse((point[0] - 7, point[1] - 7, point[0] + 7, point[1] + 7), fill="white", outline=color, width=3)
            d.text((tx, ty), name, font=font(lang, 27, True), fill=color)
        for row, (binding, title, detail) in enumerate(words[key + "_rows"]):
            y = 796 + row * 123
            d.rounded_rectangle((x + 24, y, x + 776, y + 107), 14, fill="#f1f5fa")
            d.rounded_rectangle((x + 38, y + 18, x + 292, y + 89), 12, fill=color)
            bf = fit(d, lang, binding, 27, 232, True)
            bw = d.textlength(binding, font=bf)
            d.text((x + 165 - bw / 2, y + 34), binding, font=bf, fill="white")
            d.text((x + 312, y + 11), title, font=fit(d, lang, title, 31, 440, True), fill=ink)
            d.text((x + 312, y + 61), detail, font=fit(d, lang, detail, 24, 440), fill=muted)
    d.text((70, 1335), words["fingers"], font=fit(d, lang, words["fingers"], 26, 1640), fill=muted)
    d.rounded_rectangle((60, 1390, 1740, 1553), 18, fill="#e0eaf4")
    d.text((84, 1403), words["note_title"], font=fit(d, lang, words["note_title"], 29, 1632, True), fill=ink)
    d.text((84, 1453), words["note"], font=fit(d, lang, words["note"], 24, 1632), fill=muted)
    d.text((84, 1498), words["consent"], font=fit(d, lang, words["consent"], 24, 1632), fill=muted)
    credit = "Controller artwork © Meta · Meta Quest Hardware Art 1.8 · Mod bindings: Gakumas VR · No Meta endorsement"
    d.text((62, 1580), credit, font=font("en", 20), fill="#62758b")
    output = IMAGES / f"vr-controls-{lang}.png"
    image.save(output, optimize=True)
    print(output)


for language, translations in COPY.items():
    render(language, translations)
