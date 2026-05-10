def remove_translated_lines(file_path):
    with open(file_path, 'r', encoding='utf-8') as f:
        lines = f.readlines()
    
    new_lines = [line for line in lines if 'translated:' not in line]
    
    with open(file_path, 'w', encoding='utf-8') as f:
        f.writelines(new_lines)

remove_translated_lines('dynamicStrings.txt')