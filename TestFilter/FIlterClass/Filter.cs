using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Filter
{
	/// <summary>
	/// IIRフィルタ（直接型II転置）の実装
	/// </summary>
	class Filtering
	{
		//フィールド
		// 遅延バッファ
		private double[,] u;

		//フィルタ係数
		private FilterCoefficient _coeff;

		//プロパティ
		// フィルタ係数
		public FilterCoefficient Coeff
		{
			set
			{
				_coeff = value;

				//遅延バッファのオブジェクト作成
				u = new double[_coeff.Sections, 2];
			}
			get
			{
				return this._coeff;
			}
		}

		//メソッド

		/// <summary>
		/// コンストラクタ
		/// </summary>
		public Filtering()
		{
		}

		/// <summary>
		/// コンストラクタ（フィルタ係数をセット）
		/// </summary>
		/// <param name="coeff">フィルタ係数</param>
		public Filtering(FilterCoefficient coeff)
		{
			Coeff = coeff;
		}

		/// <summary>
		/// IIRフィルタ（直接型II転置、多段2次）
		/// </summary>
		/// <param name="x">データ</param>
		/// <returns>フィルタ後のデータ</returns>
		public double Filter(double x)
		{
			double y=0;

			for (var k = 0; k < _coeff.Sections; k++)
			{
				// 出力の計算
				y = (_coeff.Numerator[k, 0] * x + u[k, 0]) / _coeff.Denominator[k, 0];

				// 遅延バッファの計算
				u[k, 0] = _coeff.Numerator[k, 1] * x - _coeff.Denominator[k, 1] * y + u[k, 1];
				u[k, 1] = _coeff.Numerator[k, 2] * x - _coeff.Denominator[k, 2] * y;

				//次段の入力
				x = y;
			}
			//結果の出力
			return y;
		}
	}		
}
