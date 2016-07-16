/* 
 * UnityScript Lightweight XML Parser 
 * by Fraser McCormick (unityscripts@roguishness.com) 
 * http://twitter.com/flimgoblin 
 * http://www.roguishness.com/unity/ 
 * 
 * You may use this script under the terms of either the MIT License  
 * or the Gnu Lesser General Public License (LGPL) Version 3.  
 * See: 
 * http://www.roguishness.com/unity/lgpl-3.0-standalone.html 
 * http://www.roguishness.com/unity/gpl-3.0-standalone.html 
 * or 
 * http://www.roguishness.com/unity/MIT-license.txt 
 */  

///* Usage: 
// * parser=new XMLParser(); 
// * var node=parser.Parse("<example><value type=\"String\">Foobar</value><value type=\"Int\">3</value></example>"); 
// *  
// * Nodes are Boo.Lang.Hash values with text content in "_text" field, other attributes 
// * in "@attribute" and any child nodes listed in an array of their nodename. 
// *  
// * any XML meta tags <? .. ?> are ignored as are comments <!-- ... --> 
// * any CDATA is bundled into the "_text" attribute of its containing node. 
// * 
// * e.g. the above XML is parsed to: 
// * node={ "example":  
// *          [  
// *             { "_text":"",  
// *                "value": [ { "_text":"Foobar", "@type":"String"}, {"_text":"3", "@type":"Int"}] 
// *             }  
// *          ], 
// *        "_text":"" 
// *     } 
// *         
// */

using System.Collections;

namespace XmlParser
{
	public class XMLParser  
	{     
		private char LT     = '<';  
		private char GT     = '>';  
		private char SPACE  = ' ';  
		private char QUOTE  = '"';  
		private char QUOTE2 = '\'';  
		private char SLASH  = '/';  
		private char QMARK  = '?';  
		private char EQUALS = '=';  
		private char EXCLAMATION = '!';  
		private char DASH   = '-';  
		//private char SQL  = '[';  
		private char SQR    = ']';  
		
		public  XMLNode Parse(string content)  
		{  
			XMLNode rootNode = new XMLNode();  
			rootNode["_text"] = "";  
			
	//		string nodeContents = "";  
			
			bool inElement = false;  
			bool collectNodeName = false;  
			bool collectAttributeName = false;  
			bool collectAttributeValue = false;  
			bool quoted = false;  
			string attName = "";  
			string attValue = "";  
			string nodeName = "";  
			string textValue = "";  
			
			bool inMetaTag = false;  
			bool inComment = false;  
			bool inCDATA = false;  
			
			XMLNodeList parents = new XMLNodeList();  
			
			XMLNode currentNode = rootNode;  
			
			for (int i = 0; i < content.Length; i++)  
			{  
				char c = content[i];  
				char cn = '~';  // unused char  
				char cnn = '~'; // unused char  
				char cp = '~';  // unused char  
				
				if ((i + 1) < content.Length) cn = content[i + 1];   
				if ((i + 2) < content.Length) cnn = content[i + 2];   
				if (i > 0) cp = content[i - 1];  
				
				if (inMetaTag)  
				{  
					if (c == QMARK && cn == GT)  
					{  
						inMetaTag = false;  
						i++;  
					}  
					
					continue;  
				}  
				else  
				{  
					if (!quoted && c == LT && cn == QMARK)  
					{  
						inMetaTag = true;  
						continue;     
					}     
				}  
				
				if (inComment)  
				{  
					if (cp == DASH && c == DASH && cn == GT)  
					{  
						inComment = false;  
						i++;  
					}  
					
					continue;     
				}  
				else  
				{  
					if (!quoted && c == LT && cn == EXCLAMATION)  
					{  
						
						if (content.Length > i + 9 && content.Substring(i, 9) == "<![CDATA[")  
						{  
							inCDATA = true;  
							i += 8;  
						}  
						else  
						{                     
							inComment = true;  
						}  
						
						continue;     
					}  
				}  
				
				if (inCDATA)  
				{  
					if (c == SQR && cn == SQR && cnn == GT)  
					{  
						inCDATA = false;  
						i += 2;  
						continue;  
					}  
					
					textValue += c;  
					continue;     
				}  
				
				
				if (inElement)  
				{  
					if (collectNodeName)  
					{  
						if (c == SPACE)  
						{  
							collectNodeName = false;  
						}  
						else if (c == GT)  
						{  
							collectNodeName = false;  
							inElement=false;  
						}  
						
						
						
						if (!collectNodeName && nodeName.Length > 0)  
						{  
							if (nodeName[0] == SLASH)  
							{  
								// close tag  
								if (textValue.Length > 0)  
								{  
									currentNode["_text"] += textValue;  
								}  
								
								textValue = "";  
								nodeName = "";  
								currentNode = parents.Pop();  
							}  
							else  
							{  
								if (textValue.Length > 0)  
								{  
									currentNode["_text"] += textValue;  
								}  
								
								textValue = "";   
								XMLNode newNode = new XMLNode();  
								newNode["_text"] = "";  
								newNode["_name"] = nodeName;  
								
								if (currentNode[nodeName] == null)  
								{  
									currentNode[nodeName] = new XMLNodeList();    
								}  
								
								XMLNodeList a = (XMLNodeList)currentNode[nodeName];  
								a.Push(newNode);      
								parents.Push(currentNode);  
								currentNode=newNode;  
								nodeName="";  
							}  
						}  
						else  
						{  
							nodeName += c;    
						}  
					}   
					else  
					{  
						if(!quoted && c == SLASH && cn == GT)  
						{  
							inElement = false;  
							collectAttributeName = false;  
							collectAttributeValue = false;    
							if (attName.Length > 0)  
							{  
								if (attValue.Length > 0)  
								{  
									currentNode["@" + attName] = attValue;                                
								}  
								else  
								{  
									currentNode["@" + attName] = true;                                
								}  
							}  
							
							i++;  
							currentNode = parents.Pop();  
							attName = "";  
							attValue = "";        
						}  
						else if (!quoted && c == GT)  
						{  
							inElement = false;  
							collectAttributeName = false;  
							collectAttributeValue = false;    
							if (attName.Length > 0)  
							{  
								currentNode["@" + attName] = attValue;                            
							}  
							
							attName = "";  
							attValue = "";    
						}  
						else  
						{  
							if (collectAttributeName)  
							{  
								if (c == SPACE || c == EQUALS)  
								{  
									collectAttributeName = false;     
									collectAttributeValue = true;  
								}  
								else  
								{  
									attName += c;  
								}  
							}  
							else if (collectAttributeValue)  
							{  
								if (c == QUOTE || c == QUOTE2)  
								{  
									if (quoted)  
									{  
										collectAttributeValue = false;  
										currentNode["@" + attName] = attValue;                                
										attValue = "";  
										attName = "";  
										quoted = false;  
									}  
									else  
									{  
										quoted = true;    
									}  
								}  
								else  
								{  
									if (quoted)  
									{  
										attValue += c;    
									}  
									else  
									{  
										if (c == SPACE)  
										{  
											collectAttributeValue = false;    
											currentNode["@" + attName] = attValue;                                
											attValue = "";  
											attName = "";  
										}     
									}  
								}  
							}  
							else if (c == SPACE)  
							{  
								
							}  
							else  
							{  
								collectAttributeName = true;                              
								attName = "" + c;  
								attValue = "";  
								quoted = false;       
							}     
						}  
					}  
				}  
				else  
				{  
					if (c == LT)  
					{  
						inElement = true;  
						collectNodeName = true;   
					}  
					else  
					{  
						textValue += c;   
					}     
				}  
			}  
			
			return rootNode;  
		}  
	}
}