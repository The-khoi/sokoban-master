import requests
import random
import os
from urllib.parse import quote
from rembg import remove


# --- 资产与路径配置 ---
UNITY_PROJECT_PATH = r"c:\Users\Alan\OneDrive\桌面\Game\sokoban-master"
UI_EXPORT_DIR = os.path.join(UNITY_PROJECT_PATH, "Assets/Sprites/UI_Generated")


class CharacterAsset:
    """
    角色视觉资产封装类
    
    将角色的美术风格关键词与原始立绘路径绑定，
    用于指导 AI 生成风格一致的 UI 资产
    """
    
    def __init__(self, name, prompt_tags, reference_image):
        self.name = name
        self.prompt_tags = prompt_tags  # 深度解构的美术风格词
        self.reference_image = reference_image  # 原始立绘在项目中的路径


# --- 1. 原始立绘资产封装 (作为 Kiro 的参考手册) ---
# 这里我们将立绘资产的路径和对应的 Prompt 规范直接绑定
CHARACTER_DATABASE = {
    "IceFreeze": CharacterAsset(
        name="IceFreeze",
        reference_image="Assets/Art/Characters/IceFreeze_FullBody.jpg",  # 微信图片_20260507153817_319_78.jpg
        prompt_tags="pastel ice blue and white, translucent frost texture, snowflake motifs, hexagonal sharp borders, winter goggles theme"
    ),
    "FireDancer": CharacterAsset(
        name="FireDancer",
        reference_image="Assets/Art/Characters/FireDancer_FullBody.jpg",  # 微信图片_20260507160224_320_78.jpg
        prompt_tags="vibrant orange and flame red, flowing fluid fire, ember particles, dark navy armor contrast, dynamic curved lines"
    ),
    "TimeKeeper": CharacterAsset(
        name="TimeKeeper",
        reference_image="Assets/Art/Characters/TimeKeeper_FullBody.jpg",  # 微信图片_20260417094719_46_43.jpg
        prompt_tags="starry night galaxy gradient, brass gold mechanical gears, roman numerals, silver white hair texture, mystical cosmic borders"
    )
}


# --- 2. 自动化生成逻辑 ---
def generate_aligned_ui(role_name, element_type, output_name):
    """
    根据封装的角色资产，指导 AI 生成风格对齐的 UI
    
    Args:
        role_name: 角色名 (IceFreeze, FireDancer, TimeKeeper)
        element_type: UI 类型 (如 'health bar', 'skill button', 'main menu frame')
        output_name: 输出文件名 (不含扩展名)
    
    Returns:
        生成成功返回文件完整路径，失败返回 None
    """
    if role_name not in CHARACTER_DATABASE:
        print(f"❌ 数据库中未找到角色: {role_name}")
        return None
    
    asset = CHARACTER_DATABASE[role_name]
    
    # 核心：将角色特征与 UI 通用规范混合
    # 使用轻厚涂 (Painterly) 和 二次元 (Anime) 作为全局约束
    full_prompt = (
        f"{element_type} for 2d game, {asset.prompt_tags}, "
        f"anime painterly style, flat design, clean edges, masterpiece, "
        f"consistent with character art style, white background --nologo"
    )
    encoded_prompt = quote(full_prompt)
    
    seed = random.randint(0, 999999)
    
    # 使用 Flux 模型以获得更好的文字理解力和构图
    url = f"https://image.pollinations.ai/prompt/{encoded_prompt}?width=1024&height=1024&nologo=true&seed={seed}&model=flux"
    
    # 执行下载与自动化处理
    if not os.path.exists(UI_EXPORT_DIR):
        os.makedirs(UI_EXPORT_DIR)
    
    save_path = os.path.join(UI_EXPORT_DIR, f"{output_name}.png")
    
    print(f"☁️ 正在为 {role_name} 生成 {element_type}...")
    response = requests.get(url)
    
    if response.status_code == 200:
        print(f"✨ 正在执行 Alpha 抠图...")
        try:
            # 使用 rembg 自动处理背景，生成直接可用的 Sprite
            output_data = remove(response.content)
            with open(save_path, 'wb') as f:
                f.write(output_data)
            print(f"✅ 成功！资产已存入: {save_path}")
            return save_path
        except Exception as e:
            print(f"⚠️ 抠图失败: {e}")
            return None
    else:
        print("❌ API 请求失败")
        return None


def generate_batch_ui(tasks):
    """
    批量生成 UI 资产
    
    Args:
        tasks: 任务列表，每个元素为 (role_name, element_type, output_name) 元组
    
    Returns:
        成功生成的文件路径列表
    """
    results = []
    for role_name, element_type, output_name in tasks:
        result = generate_aligned_ui(role_name, element_type, output_name)
        if result:
            results.append(result)
    return results


# --- 3. Kiro 联动示例 ---
# Kiro 执行: generate_aligned_ui("IceFreeze", "circular skill icon frame", "Ice_Skill_Slot")

if __name__ == "__main__":
    # 测试示例
    generate_aligned_ui("TimeKeeper", "circular skill icon frame", "TimeKeeper_Skill_Slot")
