#ifndef _STRCONVERT_H__
#define _STRCONVERT_H__

// ×Ö·û´®×ª»»
namespace StrConv
{
	typedef unsigned long   UTF32;  /* at least 32 bits */
	typedef unsigned short  UTF16;  /* at least 16 bits */
	typedef unsigned char   UTF8;   /* typically 8 bits */
	typedef unsigned int    INT;

	// utf16start, utf16end, utf8start, utf8end
	void UTF16ToUTF8(const UTF16*, const UTF16*, UTF8* , UTF8*);
	// utf8start, utf8end, utf16start, utf16end
	void UTF8ToUTF16(const UTF8*, const UTF8*, UTF16*, UTF16*);
}

#endif
