import os

def remove_pattern_from_single_file(file_path):
    # 1. 찾고자 하는 패턴 (들여쓰기 포함)
    # 주의: 실제 파일의 들여쓰기가 공백 4칸인지 확인하세요.
    target_pattern = "    flaggedForRetranslation: true\n    flaggedMistranslation: \"Failed\""
    
    # 패턴 뒤의 줄바꿈(\n)까지 포함하여 삭제하여 빈 줄이 남지 않게 합니다.
    target_with_newline = target_pattern + "\n"
    target_without_newline = target_pattern

    if not os.path.exists(file_path):
        print(f"[Error] 파일을 찾을 수 없습니다: {file_path}")
        return

    try:
        # 2. 파일 읽기
        with open(file_path, 'r', encoding='utf-8') as f:
            content = f.read()

        # 3. 패턴이 있는지 확인 및 삭제
        if target_with_newline in content:
            new_content = content.replace(target_with_newline, "")
            print(f"-> 패턴 발견: 줄바꿈과 함께 삭제합니다.")
        elif target_without_newline in content:
            new_content = content.replace(target_without_newline, "")
            print(f"-> 패턴 발견: 문구만 삭제합니다.")
        else:
            print(f"-> 패턴을 찾지 못했습니다. (파일 내용이 패턴과 일치하지 않음)")
            return

        # 4. 파일 쓰기 (덮어쓰기)
        with open(file_path, 'w', encoding='utf-8') as f:
            f.write(new_content)
        
        print(f"[Success] 작업 완료: {file_path}")

    except Exception as e:
        print(f"[Error] 작업 중 오류 발생: {e}")

if __name__ == "__main__":
    print("--- 특정 파일 내 패턴 삭제 프로그램 ---")
    # 사용자가 파일 경로를 직접 입력하도록 함
    path = input("삭제할 파일의 경로를 입력하세요 (예: test.txt 또는 C:/data/config.txt): ").strip()
    
    # 입력받은 경로에 따옴표가 포함되어 있다면 제거 (윈도우에서 경로 복사 시 발생 방지)
    path = path.replace('"', '').replace("'", "")
    
    remove_pattern_from_single_file(path)
