/*---------------------------------------------------------------------------------------
-- 负责人: YeSiCheng
-- 创建时间: 2023-07-03 20:52:49
-- 概述:
---------------------------------------------------------------------------------------*/

using UnityEngine;
using UnityEngine.UI;

namespace Onemt.Core.UI
{
	[RequireComponent(typeof(CanvasRenderer))]
	public class EmptyRaycast : MaskableGraphic
	{
		protected EmptyRaycast()
		{
			useLegacyMeshGeneration = false;
		}

		protected override void OnPopulateMesh(VertexHelper toFill)
		{
			toFill.Clear();
		}
	}
}