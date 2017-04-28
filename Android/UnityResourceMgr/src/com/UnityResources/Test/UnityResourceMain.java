package com.UnityResources.Test;
import java.io.File;

import android.content.Context;
import android.os.Bundle;

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
		String writePath = GetWritePath();
		SendWritePath(writePath);
	}
	
	public String GetWritePath()
	{
		File f = getFilesDir();
		return f.getAbsolutePath();
	}
	
	private native void SendWritePath(String path);
	
}
