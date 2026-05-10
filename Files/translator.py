import re
import openai

# 사용자 설정 구역
API_KEY = "no-key"  # 로컬 서버라면 보통 'no-key' 또는 임의의 문자열
BASE_URL = "http://abraxas-pc.taild7f28e.ts.net:8080/v1" # OpenAI 호환 서버 주소 입력
MODEL_NAME = "gemma-4-26B-A4B-it.i1-Q4_K_M" # 사용 중인 모델명 (로컬 모델일 경우 해당 모델 경로 혹은 이름)

# 클라이언트 초기화 (base_url 파라미터 추가)
client = openai.OpenAI(
    api_key=API_KEY,
    base_url=BASE_URL
)

def translate_text(text):
    """LLM을 사용하여 텍스트를 한국어로 번역합니다."""
    if not text.strip():
        return ""
        
    try:
        response = client.chat.completions.create(
            model=MODEL_NAME,
            messages=[
                {"role": "system", "content": "주어진 텍스트를 중국어에서 한국어로 번역하세요. 장르는 무협이고 게임 텍스트입니다. 영어를 사용하지 마세요."},
                {"role": "user", "content": text}
            ],
            temperature=0.3 # 일관된 번역을 위해 온도를 낮게 설정
        )
        return response.choices[0].message.content.strip()
    except Exception as e:
        print(f"번역 오류 발생: {e}")
        return text

def process_file(input_file, output_file):
    try:
        with open(input_file, 'r', encoding='utf-8') as f:
            lines = f.readlines()
    except FileNotFoundError:
        print(f"파일을 찾을 수 없습니다: {input_file}")
        return

    processed_lines = []
    skip_count = 0

    for i in range(len(lines)):
        # 지워야 할 줄 건너뛰기
        if skip_count > 0:
            skip_count -= 1
            continue

        # 타겟 패턴 탐색
        if "flaggedForRetranslation: true" in lines[i]:
            target_text = None
            # 위로 올라가며 큰따옴표 안의 텍스트 추출
            for j in range(i - 1, -1, -1):
                match = re.search(r'"([^"]+)"', lines[j])
                if match:
                    target_text = match.group(1)
                    break
            
            if target_text:
                print(f"번역 중: {target_text}")
                translated_text = translate_text(target_text)
                
                # 들여쓰기(Indentation) 유지
                indent_match = re.match(r'\s*', lines[i])
                indent = indent_match.group(0) if indent_match else ""
                
                # 결과물 조립: translated 줄 추가
                processed_lines.append(f"{indent}translated: \"{translated_text}\"\n")
                
                # flaggedForRetranslation 줄과 그 아래 줄(flaggedMistranslation) 삭제
                skip_count = 1 
                continue
        
        processed_lines.append(lines[i])

    with open(output_file, 'w', encoding='utf-8') as f:
        f.writelines(processed_lines)
    print(f"처리가 완료되었습니다. 결과 파일: {output_file}")

# 실행부
if __name__ == "__main__":
    process_file('dumpedPrefabText.txt', 'output.txt')