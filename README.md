# HuaweiBootloader_Bruteforce
Fastboot OEM Unlock Bruteforce Software

## Summary
  
After closing the official HUAWEI website, which allowed you to retrieve the code to unlock the bootloader of Huawei/Honor phones, here is a Winbdows C# Program to retrieve it by yourself.
It uses a bruteforce method, based on the Luhn algorithm and the iMEI identifier used by the manufacturer to generate the unlocking code

## Instructions

1. Enable developer options in Android.  
2. Enable OEM Unlock option in developer options.  
3. Connect a device in FASTBOOT mode
4. Connect your device to the computer and launch the program.  
5. You must provide your device IMEI to the program.
6. Wait it out to finish going through about 200k possible unlock codes, it's going to take a while.
