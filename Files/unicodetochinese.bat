@echo off
setlocal
title Unicode Escape Converter

:loop
echo.
echo ===========================================================
echo [Unicode Escape 변환기]
echo 변환할 유니코드(예: \u54C8\u54C8)를 입력하고 엔터를 누르세요.
echo (종료하려면 창을 닫거나 Ctrl+C를 누르세요.)
echo ===========================================================
set /p input="입력: "

if "%input%"=="" goto loop

echo.
echo 변환 결과:
powershell -NoProfile -ExecutionPolicy Bypass -Command "[regex]::Unescape('%input%')"
echo.
echo ===========================================================
goto loop