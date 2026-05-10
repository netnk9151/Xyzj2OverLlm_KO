import yaml
import os

# 경로 설정 시 앞에 r을 붙여서 \ 문제를 방지합니다.
search_path = r"F:\kobold\openlumara\openlumara\sandbox\Xyzj2OverLlm-master\Files\Converted"

def scan_directory(path):
    print(f"🔍 스캔 시작: {path}")
    for root, dirs, files in os.walk(path):
        for file in files:
            if file.endswith((".txt", ".yaml", ".yml")): # 검사할 확장자
                file_path = os.path.join(root, file)
                try:
                    with open(file_path, 'r', encoding='utf-8') as f:
                        yaml.safe_load(f)
                except yaml.YAMLError as e:
                    print(f"❌ [에러 발견] {file_path}")
                    print(f"    사유: {e}")
                except Exception as e:
                    print(f"    기타 에러 ({file}): {e}")

scan_directory(search_path)