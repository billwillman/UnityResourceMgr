#include "StrConvert.h"

namespace StrConv
{
	#define UTF8_ONE_START      (0xOOO1)
	#define UTF8_ONE_END        (0x007F)
	#define UTF8_TWO_START      (0x0080)
	#define UTF8_TWO_END        (0x07FF)
	#define UTF8_THREE_START    (0x0800)
	#define UTF8_THREE_END      (0xFFFF)

void UTF16ToUTF8(const UTF16* pUTF16Start, const UTF16* pUTF16End, UTF8* pUTF8Start, UTF8* pUTF8End)
{
    const UTF16* pTempUTF16 = pUTF16Start;
    UTF8* pTempUTF8 = pUTF8Start;

    while (pTempUTF16 < pUTF16End)
    {
        if (*pTempUTF16 <= UTF8_ONE_END
            && pTempUTF8 + 1 < pUTF8End)
        {
            //0000 - 007F  0xxxxxxx
            *pTempUTF8++ = (UTF8)*pTempUTF16;
        }
        else if(*pTempUTF16 >= UTF8_TWO_START && *pTempUTF16 <= UTF8_TWO_END
            && pTempUTF8 + 2 < pUTF8End)
        {
            //0080 - 07FF 110xxxxx 10xxxxxx
            *pTempUTF8++ = (*pTempUTF16 >> 6) | 0xC0;
            *pTempUTF8++ = (*pTempUTF16 & 0x3F) | 0x80;
        }
        else if(*pTempUTF16 >= UTF8_THREE_START && *pTempUTF16 <= UTF8_THREE_END
            && pTempUTF8 + 3 < pUTF8End)
        {
            //0800 - FFFF 1110xxxx 10xxxxxx 10xxxxxx
            *pTempUTF8++ = (*pTempUTF16 >> 12) | 0xE0;
            *pTempUTF8++ = ((*pTempUTF16 >> 6) & 0x3F) | 0x80;
            *pTempUTF8++ = (*pTempUTF16 & 0x3F) | 0x80;
        }
        else
        {
            break;
        }
        pTempUTF16++;
    }
    *pTempUTF8 = 0;
}

void UTF8ToUTF16(const UTF8* pUTF8Start, const UTF8* pUTF8End, UTF16* pUTF16Start, UTF16* pUTF16End)
{
    UTF16* pTempUTF16 = pUTF16Start;
    const UTF8* pTempUTF8 = pUTF8Start;

    while (pTempUTF8 < pUTF8End && pTempUTF16+1 < pUTF16End)
    {
        if (*pTempUTF8 >= 0xE0 && *pTempUTF8 <= 0xEF)//是3个字节的格式
        {
            //0800 - FFFF 1110xxxx 10xxxxxx 10xxxxxx
            *pTempUTF16 |= ((*pTempUTF8++ & 0xEF) << 12);
            *pTempUTF16 |= ((*pTempUTF8++ & 0x3F) << 6);
            *pTempUTF16 |= (*pTempUTF8++ & 0x3F);

        }
        else if (*pTempUTF8 >= 0xC0 && *pTempUTF8 <= 0xDF)//是2个字节的格式
        {
            //0080 - 07FF 110xxxxx 10xxxxxx
            *pTempUTF16 |= ((*pTempUTF8++ & 0x1F) << 6);
            *pTempUTF16 |= (*pTempUTF8++ & 0x3F);
        }
        else if(*pTempUTF8 >= 0 && *pTempUTF8 <= 0x7F)//是1个字节的格式
        {
            //0000 - 007F  0xxxxxxx
            *pTempUTF16 = *pTempUTF8++;
        }
        else
        {
            break;
        }
        pTempUTF16++;
    }
    *pTempUTF16 = 0;
}
}
