/********************************************************           
BEDROCKFRAMEWORK : https://github.com/GainDeveloper/BedrockFramework
********************************************************/

using UnityEngine;
using ProtoBuf;

namespace BedrockFramework.Saves
{
	[ProtoContract]
	public class SaveableVector3
	{
			[ProtoMember(1)]
			public float x;
			
			[ProtoMember(2)]
			public float y;
			
			[ProtoMember(3)]
			public float z;
			
			public SaveableVector3()
			{
					this.x = 0.0f;
					this.y = 0.0f;
					this.z = 0.0f;
			}
			
			public SaveableVector3(float x, float y, float z)
			{
					this.x = x;
					this.y = y;
					this.z = z;
			}
			
			public static implicit operator Vector3(SaveableVector3 v)
			{
					return new Vector3(v.x, v.y, v.z);
			}
			
			public static implicit operator SaveableVector3(Vector3 v)
			{
					return new SaveableVector3(v.x, v.y, v.z);
			}
	}
		 
	[ProtoContract]
	public class MyQuaternion
	{
			[ProtoMember(1)]
			public float x;
			
			[ProtoMember(2)]
			public float y;
			
			[ProtoMember(3)]
			public float z;
				   
			[ProtoMember(4)]
			public float w;
			
			public MyQuaternion()
			{
					this.x = 0.0f;
					this.y = 0.0f;
					this.z = 0.0f;
					this.w = 0.0f;
			}
			
			public MyQuaternion(float x, float y, float z, float w)
			{
					this.x = x;
					this.y = y;
					this.z = z;
					this.w = w;
			}
			
			public static implicit operator Quaternion(MyQuaternion v)
			{
					return new Quaternion(v.x, v.y, v.z, v.w);
			}
			
			public static implicit operator MyQuaternion(Quaternion v)
			{
					return new MyQuaternion(v.x, v.y, v.z, v.w);
			}
	}
}