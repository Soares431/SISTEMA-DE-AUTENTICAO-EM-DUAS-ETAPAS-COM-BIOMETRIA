$arduinoCli = "C:\Users\gp254\AppData\Local\Programs\Arduino IDE\resources\app\lib\backend\resources\arduino-cli.exe"
& $arduinoCli compile --fqbn arduino:avr:uno --output-dir build Arduino.ino
Write-Host "Compilado!" -ForegroundColor Green
