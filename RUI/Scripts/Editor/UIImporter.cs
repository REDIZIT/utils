#if UNITY_EDITOR
using System.IO;
using UnityEditor.AssetImporters;
using UnityEngine;

namespace InGame.UI.Editor
{
	[ScriptedImporter(1, "ui")]
	public class UIImporter : ScriptedImporter
	{
		public override void OnImportAsset(AssetImportContext ctx)
		{
			string fileContent = File.ReadAllText(ctx.assetPath);
			TextAsset subAsset = new TextAsset(fileContent);

			// Регистрируем текст как главный объект ассета
			ctx.AddObjectToAsset("MainText", subAsset);
			ctx.SetMainObject(subAsset);
		}
	}
}
#endif