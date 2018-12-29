using System;
using System.Collections.Generic;
using UnityEngine;

namespace LitJson
{
	public class JsonExtend
	{
		public static void AddExtentds()
		{
			// Vector4 exporter
			ExporterFunc<Vector4> vector4Exporter = new ExporterFunc<Vector4>(JsonExtend.vector4exp);
			JsonMapper.RegisterExporter<Vector4>(vector4Exporter);
			
			// Vector3 exporter
			ExporterFunc<Vector3> vector3Exporter = new ExporterFunc<Vector3>(JsonExtend.vector3exp);
			JsonMapper.RegisterExporter<Vector3>(vector3Exporter);
			
			// Vector2 exporter
			ExporterFunc<Vector2> vector2Exporter = new ExporterFunc<Vector2>(JsonExtend.vector2exp);
			JsonMapper.RegisterExporter<Vector2>(vector2Exporter);
			
			// float to double
			ExporterFunc<float> float2double = new ExporterFunc<float>(JsonExtend.float2double);
			JsonMapper.RegisterExporter<float>(float2double);
			
			// double to float
			ImporterFunc<double, Single> double2float = new ImporterFunc<double, Single>(JsonExtend.double2float);
			JsonMapper.RegisterImporter<double, Single>(double2float);

			// string to vector3
			ImporterFunc<string, Vector3> string2vector3 = new ImporterFunc<string, Vector3>(JsonExtend.string2vector3);
			JsonMapper.RegisterImporter<string, Vector3>(string2vector3);

            // string to int
            ImporterFunc<string, int> string2int = new ImporterFunc<string, int>(JsonExtend.string2int);
            JsonMapper.RegisterImporter<string, int>(string2int);

            JsonMapper.RegisterImporter<int, long>((int value) => {
                return (long)value;
            });

            JsonMapper.RegisterImporter<uint, Int64>((uint value) => {
                return (Int64)value;
            });
        }
        public static int string2int(string value) {
            int result = 0;
            int.TryParse(value, out result);
            return result;
        }
        public static void vector4exp(Vector4 value, JsonWriter writer)
		{
			writer.WriteObjectStart();
			writer.WritePropertyName("x");
			writer.Write(value.x);
			writer.WritePropertyName("y");
			writer.Write(value.y);
			writer.WritePropertyName("z");
			writer.Write(value.z);
			writer.WritePropertyName("w");
			writer.Write(value.w);
			writer.WriteObjectEnd();
		}
		
		public static void vector3exp(Vector3 value, JsonWriter writer)
		{
			writer.WriteObjectStart();
			writer.WritePropertyName("x");
			writer.Write(value.x);
			writer.WritePropertyName("y");
			writer.Write(value.y);
			writer.WritePropertyName("z");
			writer.Write(value.z);
			writer.WriteObjectEnd();
		}
		
		public static void vector2exp(Vector2 value, JsonWriter writer)
		{
			writer.WriteObjectStart();
			writer.WritePropertyName("x");
			writer.Write(value.x);
			writer.WritePropertyName("y");
			writer.Write(value.y);
			writer.WriteObjectEnd();
		}
		
		public static void float2double(float value, JsonWriter writer)
		{
			writer.Write((double)value);
		}
		
		public static System.Single double2float(double value)
		{
			return (System.Single)value;
		}

		public static Vector3 string2vector3(string value)
		{
			string[] s = value.Split(',');
			return new Vector3(float.Parse(s[0]), float.Parse(s[1]), float.Parse(s[2]));
		}
	}
}
