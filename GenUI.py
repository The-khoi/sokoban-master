import requests
import random
import os
from urllib.parse import quote
from rembg import remove  # 导入抠图库


def generate_ui_asset(prompt, file_name, project_path):
    """
    使用 Pollinations AI 生成 2D 游戏 UI 资产，并自动去除背景
    
    Args:
        prompt: 描述资产的提示词
        file_name: 保存的文件名（不含扩展名）
        project_path: Unity 项目根路径

    Returns:
        生成成功返回文件完整路径，失败返回 None
    """
    # 1. 针对 2D 游戏 UI 的 Prompt 增强
    full_prompt = f"{prompt}, 2d game ui, isometric, white background, high detail, masterpiece, flat design --nologo"
    encoded_prompt = quote(full_prompt)

    # 2. 拼接 Pollinations API URL
    seed = random.randint(0, 999999)
    url = f"https://image.pollinations.ai/prompt/{encoded_prompt}?width=1024&height=1024&nologo=true&seed={seed}&model=flux"

    # 3. 确定 Unity 项目路径
    target_dir = os.path.join(project_path, "Assets/Sprites/UI_Gen")
    if not os.path.exists(target_dir):
        os.makedirs(target_dir)

    save_path = os.path.join(target_dir, f"{file_name}.png")

    print(f"☁️ 正在从云端调取美术资产: {file_name}...")
    response = requests.get(url)

    if response.status_code == 200:
        # 先保存带背景的原始图片
        with open(save_path, "wb") as f:
            f.write(response.content)

        # 4. 核心抠图逻辑
        print("✨ 正在自动去除背景，提取透明 Sprite...")
        try:
            with open(save_path, 'rb') as input_file:
                input_data = input_file.read()

            output_data = remove(input_data)  # 调用 rembg 剥离背景

            # 将抠好的透明图片覆盖写入原路径
            with open(save_path, 'wb') as output_file:
                output_file.write(output_data)

            print(f"✅ 资产已送达并完成抠图: {save_path}")
            return save_path

        except Exception as e:
            print(f"⚠️ 抠图环节出现异常，保留原图。报错信息: {e}")
            return save_path
    else:
        print("❌ 生成失败，请检查网络连接")
        return None


# --- 测试代码 ---
# 如果你要直接运行这个 python 脚本测试，取消下面这行的注释
# generate_ui_asset("FireDancer skill icon, elegant flames, red and gold", "ui_FireDancer_Skill", "D:/UnityProjects/MyGame")
