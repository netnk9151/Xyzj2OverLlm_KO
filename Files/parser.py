import re

def extract_pattern_content(input_file_path, output_file_path):
    # 정규식 설명:
    # text:\s      -> 'text: ' 로 시작
    # ("[^"]+")    -> [그룹 1] 큰따옴표와 그 안의 내용을 캡처 (우리가 가져올 부분)
    # \r?\n-       -> 줄바꿈 후 하이픈(-)이 오는 패턴을 확인 (필터링 조건)
    # (re.DOTALL을 쓰지 않으므로 \n은 줄바꿈을 의미합니다)
    pattern = r'text:\s("[^"]+")\r?\n-'

    try:
        with open(input_file_path, 'r', encoding='utf-8') as f:
            content = f.read()

        # re.findall은 괄호()로 지정된 그룹(그룹 1)만 리스트로 반환합니다.
        # 즉, 전체 패턴이 일치해야 하지만 결과값은 "[^"]+" 부분만 가져옵니다.
        matches = re.findall(pattern, content)

        if matches:
            with open(output_file_path, 'w', encoding='utf-8') as out_f:
                for match in matches:
                    out_f.write(match + '\n')
            print(f"성공: {len(matches)}개의 항목을 '{output_file_path}'에 저장했습니다.")
        else:
            print("조건에 일치하는 텍스트를 찾지 못했습니다.")
            print("패턴 예시: text: \"내용\"\n-")

    except FileNotFoundError:
        print(f"에러: '{input_file_path}' 파일을 찾을 수 없습니다.")
    except Exception as e:
        print(f"에러 발생: {e}")

# --- 설정 부분 ---
input_filename = 'input.txt'   # 읽을 파일명
output_filename = 'output.txt' # 결과 파일명

if __name__ == "__main__":
    extract_pattern_content(input_filename, output_filename)
