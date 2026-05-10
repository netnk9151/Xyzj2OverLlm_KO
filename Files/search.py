import yaml
import sys

def check_yaml_syntax(file_path):
    try:
        with open(file_path, 'r', encoding='utf-8') as f:
            # yaml.safe_load를 사용하여 전체 파일을 파싱합니다.
            yaml.safe_load(f)
        print(f"✅ '{file_path}'는 유효한 YAML 양식입니다.")
        
    except yaml.YAMLError as exc:
        print(f"❌ YAML 양식 오류를 발견했습니다.")
        
        # 에러 위치 정보가 있는 경우 상세히 출력
        if hasattr(exc, 'problem_mark'):
            mark = exc.problem_mark
            print(f"---")
            print(f"📍 위치: {mark.line + 1}행, {mark.column + 1}열")
            print(f"🔍 원인: {exc.problem}")
            if exc.context:
                print(f"💡 문맥: {exc.context}")
            print(f"---")
            
            # 해당 라인을 읽어서 시각적으로 표시
            show_error_line(file_path, mark.line)
        else:
            print(f"상세 에러 정보: {exc}")

def show_error_line(file_path, error_line_idx):
    try:
        with open(file_path, 'r', encoding='utf-8') as f:
            lines = f.readlines()
            start = max(0, error_line_idx - 2)
            end = min(len(lines), error_line_idx + 3)
            
            print("\n[오류 주변 코드 스니펫]")
            for i in range(start, end):
                prefix = ">> " if i == error_line_idx else "   "
                print(f"{prefix}{i + 1:3}: {lines[i].rstrip()}")
    except Exception as e:
        print(f"파일 내용을 읽는 중 오류 발생: {e}")

if __name__ == "__main__":
    path = input("검사할 YAML 파일 경로를 입력하세요: ").strip().strip('"')
    check_yaml_syntax(path)