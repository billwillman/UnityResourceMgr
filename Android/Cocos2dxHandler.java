package com.example.crossgate;

import java.lang.ref.WeakReference;

import android.app.Activity;
import android.app.AlertDialog;
import android.content.DialogInterface;
import android.os.Handler;
import android.os.Message;
import android.util.Log;

public class Cocos2dxHandler extends Handler {
	// ===========================================================
	// Constants
	// ===========================================================
	public final static int HANDLER_SHOW_DIALOG = 1;
	public final static int HANDLER_SHOW_EDITBOX_DIALOG = 2;
	
	// ===========================================================
		// Fields
		// ===========================================================
		private WeakReference<Activity> mActivity;
		
		// ===========================================================
		// Constructors
		// ===========================================================
		public Cocos2dxHandler(Activity activity) {
			this.mActivity = new WeakReference<Activity>(activity);
		}
	
	public void handleMessage(Message msg)
	{
		switch (msg.what) {
		case Cocos2dxHandler.HANDLER_SHOW_DIALOG:
			// ²»Ö§³Ö
			//showDialog(msg);
			break;
		case Cocos2dxHandler.HANDLER_SHOW_EDITBOX_DIALOG:
			showEditBoxDialog(msg);
			break;
		}
	}
	
	private void showEditBoxDialog(Message msg) {
	//	Log.i("Unity", "showEditBoxDialog");
		
		EditBoxMessage editBoxMessage = (EditBoxMessage)msg.obj;
		Cocos2dxEditBoxDialog dialog = new Cocos2dxEditBoxDialog(this.mActivity.get(),
				editBoxMessage.title,
				editBoxMessage.content,
				editBoxMessage.inputMode,
				editBoxMessage.inputFlag,
				editBoxMessage.returnType,
				editBoxMessage.maxLength);
		
		dialog.InitUnityMethod(editBoxMessage.gameObjName, editBoxMessage.methodName);
		dialog.show();
	}
	
	public static class EditBoxMessage {
		public String title;
		public String content;
		public int inputMode;
		public int inputFlag;
		public int returnType;
		public int maxLength;
		
		public String gameObjName;
		public String methodName;
		
		public EditBoxMessage(String title, String content, int inputMode, int inputFlag, int returnType, int maxLength, String gameObj, String method){
			this.content = content;
			this.title = title;
			this.inputMode = inputMode;
			this.inputFlag = inputFlag;
			this.returnType = returnType;
			this.maxLength = maxLength;
			this.gameObjName = gameObj;
			this.methodName = method;
		}
	}
}
