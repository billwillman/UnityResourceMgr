using System;
using System.Runtime.InteropServices;

public class ShareMemLib {

    [DllImport("user32.dll", CharSet = CharSet.Auto)]

    public static extern IntPtr SendMessage(IntPtr hWnd, int Msg, int wParam, IntPtr lParam);



    [DllImport("Kernel32.dll", CharSet = CharSet.Auto)]

    public static extern IntPtr CreateFileMapping(int hFile, IntPtr lpAttributes, uint flProtect, uint dwMaxSizeHi, uint dwMaxSizeLow, string lpName);



    [DllImport("Kernel32.dll", CharSet = CharSet.Auto)]

    public static extern IntPtr OpenFileMapping(int dwDesiredAccess, [MarshalAs(UnmanagedType.Bool)] bool bInheritHandle, string lpName);



    [DllImport("Kernel32.dll", CharSet = CharSet.Auto)]

    public static extern IntPtr MapViewOfFile(IntPtr hFileMapping, uint dwDesiredAccess, uint dwFileOffsetHigh, uint dwFileOffsetLow, uint dwNumberOfBytesToMap);



    [DllImport("Kernel32.dll", CharSet = CharSet.Auto)]

    public static extern bool UnmapViewOfFile(IntPtr pvBaseAddress);



    [DllImport("Kernel32.dll", CharSet = CharSet.Auto)]

    public static extern bool CloseHandle(IntPtr handle);



    [DllImport("kernel32", EntryPoint = "GetLastError")]

    public static extern int GetLastError();



    const int ERROR_ALREADY_EXISTS = 183;



    const int FILE_MAP_COPY = 0x0001;

    const int FILE_MAP_WRITE = 0x0002;

    const int FILE_MAP_READ = 0x0004;

    const int FILE_MAP_ALL_ACCESS = 0x0002 | 0x0004;



    const int PAGE_READONLY = 0x02;

    const int PAGE_READWRITE = 0x04;

    const int PAGE_WRITECOPY = 0x08;

    const int PAGE_EXECUTE = 0x10;

    const int PAGE_EXECUTE_READ = 0x20;

    const int PAGE_EXECUTE_READWRITE = 0x40;



    const int SEC_COMMIT = 0x8000000;

    const int SEC_IMAGE = 0x1000000;

    const int SEC_NOCACHE = 0x10000000;

    const int SEC_RESERVE = 0x4000000;



    const int INVALID_HANDLE_VALUE = -1;



    IntPtr m_hSharedMemoryFile = IntPtr.Zero;

    IntPtr m_pwData = IntPtr.Zero;

    bool m_bAlreadyExist = false;

    bool m_bInit = false;

    long m_MemSize = 0;



    public ShareMemLib() {

    }

    ~ShareMemLib() {

        Close();

    }



    //初始化内存

    public int Init(string strName, long lngSize) {

        if (lngSize <= 0 || lngSize > 0x00800000) lngSize = 0x00800000;

        m_MemSize = lngSize;

        if (strName.Length > 0) {

            //创建内存共享体(INVALID_HANDLE_VALUE)

            m_hSharedMemoryFile = CreateFileMapping(INVALID_HANDLE_VALUE, IntPtr.Zero, (uint)PAGE_READWRITE, 0, (uint)lngSize, strName);

            if (m_hSharedMemoryFile == IntPtr.Zero) {

                m_bAlreadyExist = false;

                m_bInit = false;

                return 2; //创建共享体失败

            } else {

                if (GetLastError() == ERROR_ALREADY_EXISTS)  //已经创建

                {

                    m_bAlreadyExist = true;

                } else {

                    m_bAlreadyExist = false;

                }

            }



            //创建内存映射

            m_pwData = MapViewOfFile(m_hSharedMemoryFile, FILE_MAP_WRITE, 0, 0, (uint)lngSize);

            if (m_pwData == IntPtr.Zero) {

                m_bInit = false;

                CloseHandle(m_hSharedMemoryFile);

                return 3; //创建内存映射失败

            } else {

                m_bInit = true;

                if (m_bAlreadyExist == false) {



                }

            }

        } else {

            return 1;

        }



        return 0;

    }

    //关闭共享内存

    public void Close() {

        if (m_bInit) {

            UnmapViewOfFile(m_pwData);

            CloseHandle(m_hSharedMemoryFile);

        }

    }

    ///读数据





    public int Read(ref byte[] bytData, int lngAddr, int lngSize) {

        if (lngAddr + lngSize > m_MemSize) return 2; //超出数据区

        if (m_bInit) {

            Marshal.Copy(m_pwData, bytData, lngAddr, lngSize);

        } else {

            return 1;

        }

        return 0;

    }



    //将数据写入共享内存中

    public int Write(byte[] bytData, int lngAddr, int lngSize) {

        if (lngAddr + lngSize > m_MemSize) return 2; //超出数据区

        if (m_bInit) {

            Marshal.Copy(bytData, lngAddr, m_pwData, lngSize);

        } else {

            return 1;

        }

        return 0;

    }

}