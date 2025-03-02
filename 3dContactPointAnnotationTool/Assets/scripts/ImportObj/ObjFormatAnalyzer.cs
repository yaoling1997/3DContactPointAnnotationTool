﻿using System;
using System.Linq;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Hont
{
    public class ObjFormatAnalyzer
    {
        public struct Vector
        {
            public float X;
            public float Y;
            public float Z;
        }

        public struct FacePoint
        {
            public int VertexIndex;
            public int TextureIndex;
            public int NormalIndex;
        }

        public struct Face
        {
            public FacePoint[] Points;
            public bool IsQuad;
        }

        public Vector[] VertexArr;
        public Vector[] VertexNormalArr;
        public Vector[] VertexTextureArr;
        public Face[] FaceArr;
        public int[] ObjFaceBeginArr;
        public string[] ObjNameArr;

        public void Analyze(string content)
        {
            content = content.Replace("\r", string.Empty).Replace('\t', ' ');

            var lines = content.Split('\n');
            var vertexList = new List<Vector>();
            var vertexNormalList = new List<Vector>();
            var vertexTextureList = new List<Vector>();
            var faceList = new List<Face>();
            var objFaceBeginList = new List<int>();
            var objNameList = new List<string>();
            string objName = "";//物体的名称
            int defaultObjNameNum = 0;//默认物体名称编号
            //int dealtFaceNum = 0;//已处理的面数

            for (int i = 0; i < lines.Length; i++)
            {
                var currentLine = lines[i];
                //Debug.Log("current line: " + i);
                //Debug.Log("content: " + currentLine);
                //Debug.Log("size: " + currentLine.Length);
                if (currentLine.Contains("#") || currentLine.Length == 0)
                {
                    continue;
                }

                if (currentLine.Contains("v "))
                {
                    var splitInfo = currentLine.Split(" ".ToCharArray(), StringSplitOptions.RemoveEmptyEntries);
                    vertexList.Add(new Vector() { X = float.Parse(splitInfo[1]), Y = float.Parse(splitInfo[2]), Z = float.Parse(splitInfo[3]) });
                }
                else if (currentLine.Contains("vt "))
                {
                    var splitInfo = currentLine.Split(" ".ToCharArray(), StringSplitOptions.RemoveEmptyEntries);
                    vertexTextureList.Add(new Vector() { X = float.Parse(splitInfo[1]), Y = float.Parse(splitInfo[2]), Z = 3 < splitInfo.Length ? float.Parse(splitInfo[3]):0 });
                }
                else if (currentLine.Contains("vn "))
                {
                    var splitInfo = currentLine.Split(" ".ToCharArray(), StringSplitOptions.RemoveEmptyEntries);
                    vertexNormalList.Add(new Vector() { X = float.Parse(splitInfo[1]), Y = float.Parse(splitInfo[2]), Z = float.Parse(splitInfo[3]) });
                }
                else if (currentLine.Contains("f "))
                {
                    //Debug.Log("currentLine:"+ currentLine);
                    var splitInfo = currentLine.Split(" ".ToCharArray(), StringSplitOptions.RemoveEmptyEntries);
                    //Debug.Log("splitInfo length:"+splitInfo.Length);                    
                    var isQuad = splitInfo.Length > 4;
                    var face1 = splitInfo[1].Split('/');
                    var face2 = splitInfo[2].Split('/');
                    var face3 = splitInfo[3].Split('/');
                    var face4 = isQuad ? splitInfo[4].Split('/') : null;
                    var face = new Face();
                    face.Points = new FacePoint[4];
                    face.Points[0] = new FacePoint() { VertexIndex = StringToInt(face1[0]), TextureIndex = 1 < face1.Length? StringToInt(face1[1]):0, NormalIndex = 2 < face1.Length ? StringToInt(face1[2]):0 };
                    face.Points[1] = new FacePoint() { VertexIndex = StringToInt(face2[0]), TextureIndex = 1 < face2.Length? StringToInt(face2[1]):0, NormalIndex = 2 < face2.Length ? StringToInt(face2[2]):0 };
                    face.Points[2] = new FacePoint() { VertexIndex = StringToInt(face3[0]), TextureIndex = 1 < face3.Length? StringToInt(face3[1]):0, NormalIndex = 2 < face3.Length ? StringToInt(face3[2]):0 };
                    face.Points[3] = isQuad ? new FacePoint() { VertexIndex = StringToInt(face4[0]), TextureIndex = 1 < face4.Length ? StringToInt(face4[1]):0, NormalIndex = 2 < face4.Length ? StringToInt(face4[2]):0 } : default(FacePoint);
                    face.IsQuad = isQuad;

                    faceList.Add(face);
                }
                else if (currentLine.Contains("o ")) {
                    if (objFaceBeginList.Count==0 && faceList.Count!=0) {
                        objName = "default" + defaultObjNameNum;
                        defaultObjNameNum++;
                        objFaceBeginList.Add(0);
                        objNameList.Add(objName);                        
                    }
                    objName = "";
                    int p = currentLine.IndexOf('o');
                    int length = currentLine.Length;
                    for (p++; currentLine[p] == ' '; p++) ;
                    for (; p < length; p++)
                        objName += currentLine[p];
                    if (objFaceBeginList.Count > 0 && faceList.Count == objFaceBeginList[objFaceBeginList.Count - 1]) {//前一个对象没有对应的面，直接改名字
                        objNameList[objNameList.Count - 1] = objName;
                    }
                    else
                    {
                        objFaceBeginList.Add(faceList.Count);
                        objNameList.Add(objName);
                    }
                }
            }

            
            if (objFaceBeginList.Count == 0 && faceList.Count != 0)
            {//还有剩余的面没有被处理
                objName = "default" + defaultObjNameNum;
                defaultObjNameNum++;
                objFaceBeginList.Add(0);
                objNameList.Add(objName);
            }
            if (objFaceBeginList.Count > 0 && faceList.Count == objFaceBeginList[objFaceBeginList.Count - 1])
            {//最后一个对象没有对应的面，删除
                objFaceBeginList.RemoveAt(objFaceBeginList.Count-1);
                objNameList.RemoveAt(objNameList.Count-1);
            }
            VertexArr = vertexList.ToArray();
            VertexNormalArr = vertexNormalList.ToArray();
            VertexTextureArr = vertexTextureList.ToArray();
            FaceArr = faceList.ToArray();
            ////把整个obj文件当做单个对象
            //var fbl = new List<int>();
            //var nl = new List<string>();
            //fbl.Add(objFaceBeginList[0]);
            //nl.Add(objNameList[objNameList.Count-1]);
            //ObjFaceBeginArr = fbl.ToArray();
            //ObjNameArr = nl.ToArray();
            ////
            ObjFaceBeginArr = objFaceBeginList.ToArray();
            ObjNameArr = objNameList.ToArray();
        }
        public int StringToInt(string s) {
            if (s == null || s.Length == 0)
                return 0;
            int re = 0;
            try
            {
                re= int.Parse(s);
            }
            catch {
                return 0;
            }
            return re;
        }
    }
}