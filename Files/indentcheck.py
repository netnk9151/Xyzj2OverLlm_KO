import os

def check_indentation_in_file(file_path):
    """
    파일을 읽어 들여서 들여쓰기 오류(탭과 공백 혼용)를 찾아냅니다.
    """
    errors = []
    try:
        with open(file_path, 'r', encoding='utf-8') as f:
            for line_num, line in enumerate(f, 1):
                # 줄바꿈 문자를 제외한 순수 내용만 확인
                content = line.rstrip('\n\r')
                
                # 앞부분의 공백(들여쓰기) 부분만 추출
                indentation = content[:len(content) - len(content.lstrip())]
                
                if not indentation:
                    continue

                # 탭과 공백이 섞여 있는지 검사
                has_tab = '\t' in indentation
                has_space = ' ' in indentation

                if has_tab and has_space:
                    errors.append(f"Line {line_num}: Mixed tabs and spaces detected.")
                elif has_tab:
                    # 탭만 사용하는 경우 (사용자 설정에 따라 에러로 간주할 수 있음)
                    pass 
                elif has_space:
                    # 공백만 사용하는 경우
                    pass

        return errors
    except Exception as e:
        return [f"Error reading file: {e}"]

def main(target_folder):
    print(f"--- 검사를 시작합니다: {os.path.abspath(target_folder)} ---")
    
    found_issue = False
    
    # 폴더 내 파일 탐색
    if not os.path.exists(target_folder):
        print("Error: 지정한 폴더가 존재하지 않습니다.")
        return

    for root, dirs, files in os.walk(target_folder):
        for file in files:
            if file.endswith('.txt'):
                file_path = os.path.join(root, file)
                file_errors = check_indentation_in_file(file_path)
                
                if file_errors:
                    found_issue = True
                    print(f"\n[!] 문제 발견: {file_path}")
                    for error in file_errors:
                        print(f"    - {error}")

    if not found_issue:
        print("\n[OK] 모든 .txt 파일의 들여쓰기가 일관적입니다 (혼용 없음).")
    else:
        print("\n[FAIL] 일부 파일에서 들여쓰기 오류가 발견되었습니다.")

if __name__ == "__main__":
    # 검사하고 싶은 폴더 경로를 입력하세요. '.' 은 현재 폴더를 의미합니다.
    folder_to_scan = input("검사할 폴더 경로를 입력하세요 (현재 폴더는 . 입력): ").strip()
    if not folder_to_scan:
        folder_to_scan = r"F:\kobold\openlumara\openlumara\sandbox\Xyzj2OverLlm-master\Files\Converted"
        
    main(folder_to_scan)
