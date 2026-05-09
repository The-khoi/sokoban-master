import requests
import random
import os
from urllib.parse import quote
from rembg import remove
from PIL import Image, ImageEnhance, ImageFilter  # 补丁2：质量增强


# --- 资产与路径配置 ---
UNITY_PROJECT_PATH = r"c:\Users\Alan\OneDrive\桌面\Game\sokoban-master"
UI_EXPORT_DIR = os.path.join(UNITY_PROJECT_PATH, "Assets/Sprites/UI_Generated")

# 补丁3：负面约束词 —— 永久排除低质、多余文字、畸形肢体等污染项
NEGATIVE_CONSTRAINTS = (
    "lowres, text, error, cropped, worst quality, low quality, jpeg artifacts, "
    "ugly, duplicate, morbid, mutilated, out of frame, extra fingers, mutated hands, "
    "poorly drawn hands, poorly drawn face, mutation, deformed, blurry, dehydrated, "
    "bad anatomy, bad proportions, extra limbs, cloned face, disfigured, gross proportions, "
    "malformed limbs, missing arms, missing legs, extra arms, extra legs, fused fingers, "
    "too many fingers, long neck, username, watermark, signature"
)


# ---------------------------------------------------------------------------
# 角色资产数据库
# ---------------------------------------------------------------------------

class CharacterAsset:
    """
    角色视觉资产封装类

    将角色的美术风格关键词与原始立绘路径绑定，
    用于指导 AI 生成风格一致的 UI 资产
    """

    def __init__(self, name, prompt_tags, reference_image):
        self.name = name
        self.prompt_tags = prompt_tags      # 深度解构的美术风格词
        self.reference_image = reference_image  # 原始立绘在项目中的路径


# 将立绘资产的路径和对应的 Prompt 规范直接绑定
CHARACTER_DATABASE = {
    "IceFreeze": CharacterAsset(
        name="IceFreeze",
        reference_image="Assets/Art/Characters/IceFreeze_FullBody.jpg",
        prompt_tags=(
            "pastel ice blue and white, translucent frost texture, "
            "snowflake motifs, hexagonal sharp borders, winter goggles theme"
        )
    ),
    "FireDancer": CharacterAsset(
        name="FireDancer",
        reference_image="Assets/Art/Characters/FireDancer_FullBody.jpg",
        prompt_tags=(
            "vibrant orange and flame red, flowing fluid fire, "
            "ember particles, dark navy armor contrast, dynamic curved lines"
        )
    ),
    "TimeKeeper": CharacterAsset(
        name="TimeKeeper",
        reference_image="Assets/Art/Characters/TimeKeeper_FullBody.jpg",
        prompt_tags=(
            "starry night galaxy gradient, brass gold mechanical gears, "
            "roman numerals, silver white hair texture, mystical cosmic borders"
        )
    )
}

# generate_adaptive_ui 使用的精简风格标签（更聚焦美感表达）
STYLE_TAGS = {
    "IceFreeze":  "cool translucent tones, soft frosty textures, crystalline elegance",
    "FireDancer": "warm flowing gradients, graceful flame-like curves, sophisticated ember glow",
    "TimeKeeper": "mystical cosmic palette, elegant golden filigree, ethereal star-brushed textures"
}


# ---------------------------------------------------------------------------
# 补丁2：质量增强后处理
# ---------------------------------------------------------------------------

def enhance_quality(image_path):
    """
    对生成图片执行三步后处理：锐化 → 饱和度 +15% → 对比度 +10%

    Args:
        image_path: 待处理的图片路径（原地覆盖保存）
    """
    with Image.open(image_path) as img:
        # 1. 锐化：增强边缘质感
        img = img.filter(ImageFilter.SHARPEN)

        # 2. 饱和度增强：让角色风格的色彩更鲜艳
        img = ImageEnhance.Color(img).enhance(1.15)

        # 3. 对比度增强：提升画面通透感，避免"灰蒙蒙感"
        img = ImageEnhance.Contrast(img).enhance(1.1)

        img.save(image_path)

    print(f"✨ 质量增强补丁已应用：{image_path}")


# ---------------------------------------------------------------------------
# 核心生成函数
# ---------------------------------------------------------------------------

def generate_aligned_ui(role_name, element_type, output_name):
    """
    根据 CHARACTER_DATABASE 中的完整 prompt_tags 生成风格对齐的方形 UI (1024x1024)

    Args:
        role_name:   角色名 (IceFreeze / FireDancer / TimeKeeper)
        element_type: UI 类型描述 (如 'skill button', 'health bar')
        output_name: 输出文件名（不含扩展名）

    Returns:
        成功返回文件完整路径，失败返回 None
    """
    if role_name not in CHARACTER_DATABASE:
        print(f"❌ 数据库中未找到角色: {role_name}")
        return None

    asset = CHARACTER_DATABASE[role_name]

    full_prompt = (
        f"{element_type} for 2d game, {asset.prompt_tags}, "
        f"anime painterly style, flat design, clean edges, masterpiece, "
        f"consistent with character art style, white background, "
        f"negative: {NEGATIVE_CONSTRAINTS}"  # 补丁3：注入负面约束
    )
    encoded_prompt = quote(full_prompt)
    seed = random.randint(0, 999999)
    url = f"https://image.pollinations.ai/prompt/{encoded_prompt}?width=1024&height=1024&nologo=true&seed={seed}&model=flux"

    if not os.path.exists(UI_EXPORT_DIR):
        os.makedirs(UI_EXPORT_DIR)

    save_path = os.path.join(UI_EXPORT_DIR, f"{output_name}.png")

    print(f"☁️ 正在为 [{role_name}] 生成 {element_type}...")
    response = requests.get(url)

    if response.status_code == 200:
        print(f"✨ 正在执行 Alpha 抠图...")
        try:
            output_data = remove(response.content)
            with open(save_path, 'wb') as f:
                f.write(output_data)
            enhance_quality(save_path)  # 补丁2：自动质量增强
            print(f"✅ 成功！资产已存入: {save_path}")
            return save_path
        except Exception as e:
            print(f"⚠️ 抠图失败: {e}")
            return None
    else:
        print(f"❌ API 请求失败，状态码: {response.status_code}")
        return None


def generate_adaptive_ui(role_style, element_type, output_name, width=1792, height=1024):
    """
    自适应分辨率的工业化生成函数，适合横版 Banner / 大尺寸面板

    Args:
        role_style:  角色风格名 (IceFreeze / FireDancer / TimeKeeper)
        element_type: UI 类型描述
        output_name: 输出文件名（不含扩展名）
        width:  输出宽度，默认 1792
        height: 输出高度，默认 1024

    Returns:
        成功返回文件完整路径，失败返回 None
    """
    current_style = STYLE_TAGS.get(role_style, "general anime painterly style")

    base_instruction = (
        f"{element_type} with {current_style}, "
        "elegant and serene composition, high-end anime painterly texture, "
        "soft brushstrokes, volumetric lighting, inspired by Genshin Impact aesthetics, "
        f"clean and graceful, masterpiece, white background, "
        f"negative: {NEGATIVE_CONSTRAINTS}"  # 补丁3：注入负面约束
    )
    encoded_prompt = quote(base_instruction)
    seed = random.randint(0, 999999)
    url = f"https://image.pollinations.ai/prompt/{encoded_prompt}?width={width}&height={height}&nologo=true&seed={seed}&model=flux"

    if not os.path.exists(UI_EXPORT_DIR):
        os.makedirs(UI_EXPORT_DIR)

    save_path = os.path.join(UI_EXPORT_DIR, f"{output_name}.png")

    print(f"🚀 正在以 {width}x{height} 分辨率生成 [{role_style}] 风格的 {element_type}...")
    response = requests.get(url)

    if response.status_code == 200:
        print(f"✨ 正在执行 Alpha 抠图...")
        try:
            output_data = remove(response.content)
            with open(save_path, 'wb') as f:
                f.write(output_data)
        except Exception as e:
            print(f"⚠️ 抠图失败，保留原图: {e}")
            with open(save_path, 'wb') as f:
                f.write(response.content)

        enhance_quality(save_path)  # 补丁2：自动质量增强
        print(f"✅ 成功！资产已存入: {save_path}")
        return save_path
    else:
        print(f"❌ API 请求失败，状态码: {response.status_code}")
        return None


# ---------------------------------------------------------------------------
# 补丁1：批量生成（多版本挑选，防止单张"开天窗"）
# ---------------------------------------------------------------------------

def generate_batch_ui(role_style, element_type, batch_size=3):
    """
    一次生成多个版本，供开发者挑选，防止单张图"开天窗"

    Args:
        role_style:  角色风格名 (IceFreeze / FireDancer / TimeKeeper)
        element_type: UI 类型描述
        batch_size:  生成数量，默认 3

    Returns:
        所有生成文件的路径列表（含 None 的失败项）
    """
    results = []
    for i in range(batch_size):
        filename = f"temp_{role_style}_{i}"
        print(f"\n--- 批次 {i + 1}/{batch_size} ---")
        path = generate_adaptive_ui(role_style, element_type, output_name=filename)
        results.append(path)

    print(
        f"\n✅ 批次生成完成。请在 {UI_EXPORT_DIR} 中查看，"
        f"并告诉 Kiro 哪个序号 (0-{batch_size - 1}) 最符合预期。"
    )
    return results


# ---------------------------------------------------------------------------
# Kiro 联动示例
# ---------------------------------------------------------------------------
# generate_aligned_ui("IceFreeze", "circular skill icon frame", "Ice_Skill_Slot")
# generate_adaptive_ui("TimeKeeper", "main menu background banner", "TimeKeeper_MenuBanner")
# generate_batch_ui("FireDancer", "skill button icon", batch_size=3)

if __name__ == "__main__":
    # 测试批量生成（生成 2 张供挑选）
    generate_batch_ui("TimeKeeper", "circular skill icon frame", batch_size=2)
