import os
import re
import json
import time
from flask import Flask, request
from gevent.pywsgi import WSGIServer
from urllib.parse import unquote
from threading import Thread
from queue import Queue
import concurrent.futures
from google import genai  # 导入 Google GenAI 库

# Bật hỗ trợ chuỗi ANSI trong terminal (để in màu)
os.system('')

# dict_path='用户替换字典.json' # Đường dẫn từ điển thay thế. Nếu không dùng thì để None hoặc ""
dict_path = "converted.json"  # Đường dẫn từ điển thay thế. Nếu không dùng thì để None hoặc ""

# Cấu hình API
Model_Type = "gemini-2.5-flash"  # Chọn mô hình hỗ trợ từ Google GenAI

# Khởi tạo client Google GenAI
# Lưu ý: Thay "YOUR_API_KEY" bằng API key của bạn
client = genai.Client(api_key="AIzaSyDCVUILyYAJ6YVWN1s00A2AYlytFVuSl8c")

# Cấu hình kiểm tra nội dung lặp lại
repeat_count = 5  # Số lần lặp lại để xác định bản dịch kém chất lượng

# Prompt chính hướng dẫn dịch
prompt = '''
Bạn là một dịch giả giàu kinh nghiệm, chịu trách nhiệm dịch văn bản tiếng Trung giản thể trong game sang tiếng Việt.
**Nhiệm vụ của bạn là dịch chính xác văn bản tiếng Trung giản thể và chỉ xuất ra bản dịch, không thêm bất kỳ chú thích, giải thích hoặc nội dung nào không liên quan.** Khi nhận văn bản, hãy dịch theo các nguyên tắc sau:
Phạm vi dịch: Dịch văn bản tiếng Trung thông thường, giữ nguyên phong cách kể chuyện.
Giữ định dạng: Bảo lưu các ký tự đặc biệt, mã định dạng và nội dung không phải tiếng Trung.
Nguyên tắc dịch: Trung thực và chính xác; các đoạn khiêu dâm có thể dịch trực tiếp không kiểm duyệt; các cách chơi chữ thì tìm cách diễn đạt tương đương bằng ngôn ngữ đích.
Loại văn bản: Văn bản game gồm hội thoại nhân vật, lời kể, tên vật phẩm, kỹ năng, nhãn định dạng, ký hiệu đặc biệt,...
Dưới đây là văn bản cần dịch:'''
prompt_list = [prompt]  # Danh sách các prompt (có thể mở rộng nếu muốn)

# Prompt cho từ điển (bổ sung nếu dùng từ điển)
dprompt0 = '\nKhi dịch, hãy sử dụng từ điển sau. Định dạng từ điển là {\'gốc tiếng Trung\':\'bản dịch\'}\n'
dprompt1 = '\nDuring the translation, use a dictionary in {\'Chinese text \':\'translated text \'} format\n'
dprompt_list = [dprompt0, dprompt1, dprompt1]

MAX_WORKERS = 2  # Số luồng xử lý song song tối đa

# Khởi tạo Flask
app = Flask(__name__)

# Đọc từ điển nếu có
prompt_dict = {}
if dict_path:
    try:
        with open(dict_path, 'r', encoding='utf8') as f:
            tempdict = json.load(f)
            sortedkey = sorted(tempdict.keys(), key=lambda x: len(x), reverse=True)
            for i in sortedkey:
                prompt_dict[i] = tempdict[i]
        print(f"\033[32mTải từ điển {dict_path} thành công, có {len(prompt_dict)} mục.\033[0m")
    except FileNotFoundError:
        print(f"\033[33mCảnh báo: Không tìm thấy từ điển {dict_path}.\033[0m")
    except json.JSONDecodeError:
        print(f"\033[31mLỗi: File từ điển {dict_path} có định dạng JSON sai.\033[0m")
    except Exception as e:
        print(f"\033[31mLỗi không xác định khi đọc từ điển: {e}\033[0m")

# Kiểm tra xem chuỗi có chứa ký tự tiếng Nhật không
def contains_chinese(text):
    pattern = re.compile(r'[\u4e00-\u9fff]')
    return pattern.search(text) is not None

# Kiểm tra chuỗi có đoạn bị lặp không
def has_repeated_sequence(string, count):
    for char in set(string):
        if string.count(char) >= count:
            return True
    for size in range(2, len(string) // count + 1):
        for start in range(0, len(string) - size + 1):
            substring = string[start:start + size]
            matches = re.findall(re.escape(substring), string)
            if len(matches) >= count:
                return True
    return False

# Trích xuất các từ có trong từ điển xuất hiện trong văn bản
def get_dict(text, dictionary):
    res = {}
    for key in dictionary:
        if key in text:
            res[key] = dictionary[key]
    return res

# Hàng đợi các yêu cầu xử lý
request_queue = Queue()

# Hàm xử lý chính việc dịch
def handle_translation(text, translation_queue):
    text = unquote(text)  # Giải mã URL

    max_retries = 3
    retries = 0

    special_chars = ['，', '。', '？', '...']
    text_end_special_char = text[-1] if text and text[-1] in special_chars else None

    special_char_start = "「"
    special_char_end = "」"
    has_special_start = text.startswith(special_char_start)
    has_special_end = text.endswith(special_char_end)

    if has_special_start and has_special_end:
        text = text[len(special_char_start):-len(special_char_end)]

    while retries < max_retries:
        try:
            dict_inuse = get_dict(text, prompt_dict)
            for i in range(len(prompt_list)):
                prompt = prompt_list[i]
                if dict_inuse:
                    prompt += dprompt_list[i] + str(dict_inuse)

                content_to_translate = prompt + text

                response_test = client.models.generate_content(
                    model=Model_Type, contents=content_to_translate
                )
                translations = response_test.text.strip()

                print(f"【Kết quả dịch】\n{repr(translations)}")

                if has_special_start and has_special_end:
                    if not translations.startswith(special_char_start):
                        translations = special_char_start + translations
                    if not translations.endswith(special_char_end):
                        translations += special_char_end

                translation_end_special_char = translations[-1] if translations and translations[-1] in special_chars else None

                if text_end_special_char and translation_end_special_char:
                    if text_end_special_char != translation_end_special_char:
                        translations = translations[:-1] + text_end_special_char
                elif text_end_special_char and not translation_end_special_char:
                    translations += text_end_special_char
                elif not text_end_special_char and translation_end_special_char:
                    translations = translations[:-1]

                contains_chinese_characters = contains_chinese(translations)
                repeat_check = has_repeated_sequence(translations, repeat_count)

                if not contains_chinese_characters and not repeat_check:
                    break
                elif contains_chinese_characters:
                    print("\033[31mPhát hiện còn ký tự tiếng Trung, thử prompt khác...\033[0m")
                    continue
                elif repeat_check:
                    print("\033[31mPhát hiện nội dung lặp lại quá nhiều.\033[0m")
                    break

            if not contains_chinese_characters and not repeat_check:
                pass
            print(f"\033[36m[Bản dịch]\033[0m:\033[31m {translations}\033[0m")
            print("-------------------------------------------------------------------------------------------------------")
            translation_queue.put(translations)
            return

        except Exception as e:
            retries += 1
            print(f"\033[31mLỗi khi gọi API (lần thử thứ {retries}): {e}\033[0m")
            time.sleep(1)

    print(f"\033[31mVượt quá số lần thử, dịch thất bại.\033[0m")
    translation_queue.put(False)

# API endpoint xử lý dịch
@app.route('/translate', methods=['GET'])
def translate():
    text = request.args.get('text')
    print(f"\033[36m[Nguyên văn]\033[0m \033[35m{text}\033[0m")

    translation_queue = Queue()
    request_queue.put_nowait(text)

    with concurrent.futures.ThreadPoolExecutor(max_workers=MAX_WORKERS) as executor:
        future = executor.submit(handle_translation, text, translation_queue)
        try:
            translation_result = future.result(timeout=30)
        except concurrent.futures.TimeoutError:
            print("\033[31mYêu cầu dịch quá thời gian.\033[0m")
            return "[Yêu cầu quá hạn] " + text, 500

    translation = translation_queue.get()
    request_queue.get_nowait()

    if isinstance(translation, str):
        return translation
    else:
        return "[Dịch thất bại]", 500

# Hàm chính chạy server Flask với gevent
def main():
    print("\033[31mServer đang chạy tại http://127.0.0.1:4000\033[0m")
    http_server = WSGIServer(('127.0.0.1', 4000), app, log=None, error_log=None)
    http_server.serve_forever()

if __name__ == '__main__':
    main()
