import re

def process_translation_file(input_file_path, output_cleaned_path, output_extracted_path):
    extracted_texts = []
    cleaned_lines = []
    
    try:
        with open(input_file_path, 'r', encoding='utf-8') as f:
            lines = f.readlines()
        
        skip_count = 0
        for i in range(len(lines)):
            # 제거 로직에 의해 건너뛰어야 하는 줄인지 확인
            if skip_count > 0:
                skip_count -= 1
                continue
            
            current_line = lines[i]
            
            # 1. "Failed" 조건 확인 (추출 대상)
            if i + 2 < len(lines) and \
               'flaggedForRetranslation: true' in lines[i+1] and \
               'flaggedMistranslation: "Failed"' in lines[i+2]:
                
                # 텍스트 추출
                match = re.search(r'text:\s*"(.*)"', current_line)
                if match:
                    extracted_texts.append(match.group(1))
                
                # 추출 대상은 원본 유지 (제거하지 않음)
                cleaned_lines.append(current_line)
                
            # 2. "Failed"가 아닌 다른 flag 제거 대상 확인
            elif 'flaggedForRetranslation: true' in current_line:
                # "Failed"가 아닌 사유가 있는 경우 (예: "강,刚,")
                # 현재 줄과 다음 줄을 건너뜀 (이미 cleaned_lines에 추가되지 않음)
                skip_count = 1 
                continue
            
            else:
                # 일반적인 줄은 결과물에 포함
                cleaned_lines.append(current_line)

        # 결과 저장 1: 정제된 파일 (flag 제거됨)
        with open(output_cleaned_path, 'w', encoding='utf-8') as f:
            f.writelines(cleaned_lines)
            
        # 결과 저장 2: 추출된 "Failed" 리스트
        with open(output_extracted_path, 'w', encoding='utf-8') as f:
            for text in extracted_texts:
                f.write(text + '\n')
        
        print(f"완료!")
        print(f"- 정제된 파일: {output_cleaned_path}")
        print(f"- 추출된 텍스트: {output_extracted_path} ({len(extracted_texts)}건)")

    except Exception as e:
        print(f"오류 발생: {e}")

# 실행 환경 설정
input_path = 'stringlang.txt'
cleaned_path = 'stringlang_cleaned.txt'     # 불필요한 flag가 제거된 파일
extracted_path = 'failed_texts.txt'   # Failed 텍스트만 모은 파일

process_translation_file(input_path, cleaned_path, extracted_path)