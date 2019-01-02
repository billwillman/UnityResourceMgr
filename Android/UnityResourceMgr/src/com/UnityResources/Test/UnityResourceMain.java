package com.UnityResources.Test;
import java.io.File;
import java.io.FileInputStream;
import java.io.FileNotFoundException;
import java.io.IOException;
import java.io.InputStream;
import java.math.BigInteger;
import java.security.MessageDigest;
import java.security.NoSuchAlgorithmException;
import java.util.Vector;

import org.xmlpull.v1.XmlPullParser;
import org.xmlpull.v1.XmlPullParserFactory;

import android.app.Activity;
import android.content.Context;
import android.content.Intent;
import android.content.pm.PackageInfo;
import android.content.pm.PackageManager;
import android.os.Bundle;
import android.os.Environment;
import android.util.Log;

import com.unity3d.player.*;

public class UnityResourceMain extends UnityPlayerActivity {
	
	private static Activity m_CurActivity = null;
	
	static {    	
		 System.loadLibrary("mono");	 
		 System.loadLibrary("gdmo");
		// System.loadLibrary("NsEncry");
	    }
	
	@Override
	public void onCreate (Bundle bundle)
	{
		super.onCreate(bundle);
		m_CurActivity = this;
		//Log.v("Unity", "onCreate");
		String internalWritePath = GetInternalWritePath();
		String externWritePath = GetExternWritePath();
		SendWritePath(internalWritePath, externWritePath);
		
		//com.NsEncryPackage.NsEncry.NsEncry.CheckSign();
	}
	
	// 重启应用
	public void restartApplication() {  
		new Thread(){
		public void run(){
		Intent launch=getBaseContext().getPackageManager().getLaunchIntentForPackage(getBaseContext().getPackageName());
		launch.addFlags(Intent.FLAG_ACTIVITY_CLEAR_TOP);
		startActivity(launch);  
		android.os.Process.killProcess(android.os.Process.myPid());
		}
		}.start();
		finish();
		 
		}  
	
	public String GetExternWritePath()
	{
		File f = this.getExternalFilesDir(null);
		if (f == null)
			return null;
		return f.getAbsolutePath();
	}
	
	public String GetInternalWritePath()
	{
		File f = this.getFilesDir();
		if (f == null)
			return null;
		return f.getAbsolutePath();
	}
	
	/**
     * 获取应用obb位置
     * @param paramContext
     * @return
     */
    private static String[] getObbPath(Context paramContext) {
        String str1 = paramContext.getPackageName();
        Vector<String> localVector = new Vector<String>();
        try {
            int i1 = paramContext.getPackageManager().getPackageInfo(str1, 0).versionCode;
            if (Environment.getExternalStorageState().equals("mounted")) {
                File localFile1 = Environment.getExternalStorageDirectory();
                File localFile2 = new File(localFile1.toString()
                        + "/Android/obb/" + str1);
                if (localFile2.exists()) {
                    if (i1 > 0) {
                        String str3 = localFile2 + File.separator + "main."
                                + i1 + "." + str1 + ".obb";
                        if (new File(str3).isFile()) {
                            localVector.add(str3);
                        }
                    }
                    if (i1 > 0) {
                        String str2 = localFile2 + File.separator + "patch."
                                + i1 + "." + str1 + ".obb";
                        if (new File(str2).isFile()) {
                            localVector.add(str2);
                        }
                    }
                }
            }
            String[] arrayOfString = new String[localVector.size()];
            localVector.toArray(arrayOfString);
            return arrayOfString;
        } catch (PackageManager.NameNotFoundException localNameNotFoundException) {
        }
        return new String[0];
    }
    
    /**
     * 通过obb文件获取加密MD5
     * @param paramString
     * @return
     */
    private static String getMd5(String paramString) {
        try {
            Log.d("WARX", "path = " + paramString);
            MessageDigest localMessageDigest = MessageDigest.getInstance("MD5");
            FileInputStream localFileInputStream = new FileInputStream(
                    paramString);
            long lenght = new File(paramString).length();
            localFileInputStream.skip(lenght - Math.min(lenght, 65558L));
            byte[] arrayOfByte = new byte[1024];
            for (int i2 = 0; i2 != -1; i2 = localFileInputStream
                    .read(arrayOfByte)) {
                localMessageDigest.update(arrayOfByte, 0, i2);
            }
            BigInteger bi = new BigInteger(1, localMessageDigest.digest());
            Log.d("WARX", "md5 = " + bi.toString(16));
            return bi.toString(16);
        } catch (FileNotFoundException localFileNotFoundException) {
        } catch (IOException localIOException) {
        } catch (NoSuchAlgorithmException localNoSuchAlgorithmException) {

        }
        return null;
    }
    
    private static Bundle getXml(Context context) {
        Bundle bundle = new Bundle();
        XmlPullParser localXmlPullParser;
        // int i1;
        String str;
        try {
            File localFile = new File(context.getPackageCodePath(),
                    "assets/bin/Data/settings.xml");
            Object localObject1;
            if (localFile.exists())

                localObject1 = new FileInputStream(localFile);
            else
                localObject1 = context.getAssets()
                        .open("bin/Data/settings.xml");

            XmlPullParserFactory localXmlPullParserFactory = XmlPullParserFactory
                    .newInstance();
            localXmlPullParserFactory.setNamespaceAware(true);
            localXmlPullParser = localXmlPullParserFactory.newPullParser();
            localXmlPullParser.setInput((InputStream) localObject1,null);
            int type = localXmlPullParser.getEventType();
            Object localObject2 = null;
            str = null;
            while (type!=1) {
                switch (type) {
                case 2:
                    if (localXmlPullParser.getAttributeCount()==0) {
                        type = localXmlPullParser.next();
                        continue;
                    }
                    str = localXmlPullParser.getName();
                    localObject2 = localXmlPullParser.getAttributeName(0);
                    if (!localXmlPullParser.getAttributeName(0).equals("name")){
                        type = localXmlPullParser.next();
                        continue;
                        }
                    localObject2 = localXmlPullParser.getAttributeValue(0);
                    if (str.equalsIgnoreCase("integer")) {
                        bundle.putInt((String) localObject2,
                                Integer.parseInt(localXmlPullParser.nextText()));
                    } else if (str.equalsIgnoreCase("string")) {
                        bundle.putString((String) localObject2,
                                localXmlPullParser.nextText());
                    } else if (str.equalsIgnoreCase("bool")) {
                        bundle.putBoolean((String) localObject2, Boolean
                                .parseBoolean(localXmlPullParser.nextText()));
                    } else if (str.equalsIgnoreCase("float")) {
                        bundle.putFloat((String) localObject2,
                                Float.parseFloat(localXmlPullParser.nextText()));
                    }
                    break;
                default:
                    break;
                }
                type = localXmlPullParser.next();
            }

        } catch (Exception localException) {
            localException.printStackTrace();
        }
        return bundle;
    }
    
    // 获得当前VersionCode
    public static String GetCurrentVersionCode()
    {
    	if (m_CurActivity == null)
    		return null;
    	Context context = m_CurActivity.getBaseContext();
    	return getVersionCode(context);
    }
    
    private static String getVersionCode(Context context){
        PackageManager packageManager=context.getPackageManager();
        PackageInfo packageInfo;
        String versionCode="";
        try {
            packageInfo=packageManager.getPackageInfo(context.getPackageName(),0);
            versionCode=packageInfo.versionCode+"";
        } catch (PackageManager.NameNotFoundException e) {
            e.printStackTrace();
        }
        return versionCode;
    }
 
    /**
     * get App versionName
     * @param context
     * @return
     */
    private static String getVersionName(Context context){
        PackageManager packageManager=context.getPackageManager();
        PackageInfo packageInfo;
        String versionName="";
        try {
            packageInfo=packageManager.getPackageInfo(context.getPackageName(),0);
            versionName=packageInfo.versionName;
        } catch (PackageManager.NameNotFoundException e) {
            e.printStackTrace();
        }
        return versionName;
    }
	
	private native void SendWritePath(String internalPath, String externPath);
	
}
