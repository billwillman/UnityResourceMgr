package com.UnityResources.Test;
import java.io.File;

import android.content.Context;
import android.content.Intent;
import android.os.Bundle;
import android.os.Environment;
import android.util.Log;

import com.unity3d.player.*;

public class UnityResourceMain extends UnityPlayerActivity {
	static {    	
		 System.loadLibrary("mono");	 
		 System.loadLibrary("gdmo");      
	    }
	
	@Override
	public void onCreate (Bundle bundle)
	{
		super.onCreate(bundle);
		//Log.v("Unity", "onCreate");
		String internalWritePath = GetInternalWritePath();
		String externWritePath = GetExternWritePath();
		SendWritePath(internalWritePath, externWritePath);
	}
	
	// ÷ÿ∆Ù”¶”√
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
	
	private native void SendWritePath(String internalPath, String externPath);
	
}
