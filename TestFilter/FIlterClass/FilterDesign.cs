using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Filter
{
	/// <summary>
	/// バタワースフィルタのカットオフ種類 {Low, High, BandStop, BandPass}
	/// </summary>
	enum FilterType {Low, High, BandStop, BandPass};


	/// <summary>
	/// バンドフィルタの設定方法。バンド幅＆中心周波数か、低域／高域を選択。
	/// </summary>
	enum FilterParameterSelection { BandwidthAndCenterFrequency, LowAndHighFrequency};


	/// <summary>
	/// バタワースフィルタの設計
	// 無次元カットオフ周波数 cutoff = 2*Fcutoff/Fsampling = Fcutoff/Fnyquist
	// ハイパス／ローパス指定 filterType FilterType.{Low,High,BandStop,BandPass}
	/// </summary>
	class ButterworthFilterDesign
    {
		/// <summary>
		/// 正規化バタワース多項式の極の算出
		/// </summary>
		/// <param name="order">ローパスフィルタの次数</param>
		/// <returns>double[] s平面における上半分の極の角度</returns>
		private static double[] NormalizedButterworthPoles(int order)
		{
			bool odd = order % 2 != 0;

			// 極の数（s平面の上半分）
			int numberOfPoles = (int)((order + 1) / 2);

			// 正規化バタワース多項式の極
			var poleAngles = new double[numberOfPoles];

			if (odd)
			{
				// 奇数の時
				for (var k = 0; k < numberOfPoles; k++)
				{
					// 極は、kPI/N ( k=0,1,... (order+1)/2, N:order)
					poleAngles[k] = k * Math.PI / order;
				}
			}
			else
			{
				//偶数の時
				for (var k = 0; k < numberOfPoles; k++)
				{
					// 極は(2k-1)PI/2N ( k=0,1,... order/2, N:order)
					poleAngles[k] = (2 * (k + 1) - 1) * Math.PI / 2 / order;
				}
			}
			return poleAngles;

		}


		/// <summary>
		/// 双一次変換 
		/// </summary>
		/// <param name="order">フィルタ次数</param>
		/// <param name="Wa">フィルタの正規化カットオフ周波数または中心周波数（ナイキスト周波数で正規化）</param>
		/// <param name="aAnalog">アナログフィルタの分子の係数</param>
		/// <param name="bAnalog">アナログフィルタの分母の係数</param>
		/// <param name="filterType">フィルタの種類 FilterTypeで選択</param>
		/// <returns>FilterCoefficient 双一次変換されたデジタルフィルタの係数</returns>
		private static FilterCoefficient BilinearTransform(int order, double Wa, double[,] aAnalog, double[,] bAnalog, FilterType filterType) 
		{
			// 双一次変換の格納用配列
			var a = new double[aAnalog.GetLength(0), aAnalog.GetLength(1)];
			var b = new double[bAnalog.GetLength(0), bAnalog.GetLength(1)];

			//フィルタの次数の偶数奇数判定
			bool odd = order % 2 != 0;

			// セクション数
			int sections = aAnalog.GetLength(0);

			// 双一次変換の変換係数
			double h = 1 / (Wa * Math.PI / 2);

			for (var k = 0; k < sections; k++)
			{
				double BB;

				if (k == 0 && odd && (filterType == FilterType.Low || filterType == FilterType.High))
				{
					//ローパスハイパスの奇数一次のみ、１次のセクションとなる
					// 分母の 係数b0の逆数
					BB = 1 / (h + bAnalog[0, 0]);
					a[k, 0] = BB * (aAnalog[0, 1] * h + aAnalog[0, 0]);
					a[k, 1] = BB * (-aAnalog[0, 1] * h + aAnalog[0, 0]);
					a[k, 2] = 0;

					b[k, 0] = BB * (h + bAnalog[0, 0]);
					b[k, 1] = BB * (-h + bAnalog[0, 0]);
					b[k, 2] = 0;
				}
				else
				{
					// 分母の 係数b0の逆数
					BB = 1 / (h * h + bAnalog[k, 1] * h + bAnalog[k, 2]);

					a[k, 0] = BB * (aAnalog[k, 2] * h * h + aAnalog[k, 1] * h + aAnalog[k, 0]);
					a[k, 1] = BB * 2 * (-aAnalog[k, 2] * h * h + aAnalog[k, 0]);
					a[k, 2] = BB * (aAnalog[k, 2] * h * h - aAnalog[k, 1] * h + aAnalog[k, 0]);

					b[k, 0] = BB * (h * h + bAnalog[k, 1] * h + bAnalog[k, 2]);
					b[k, 1] = BB * (-2 * h * h + 2 * bAnalog[k, 2]);
					b[k, 2] = BB * (h * h - bAnalog[k, 1] * h + bAnalog[k, 2]);
				}
			}

			//ゲインを最初のセクションに集める
			double gain = 1;
			for(var k=0; k<sections; k++)
			{
				gain *= a[k, 0];
				a[k, 2] /= a[k,0];
				a[k, 1] /= a[k, 0];
				a[k, 0] /= a[k, 0];
			}
			a[0, 0] *= gain;
			a[0, 1] *= gain;
			a[0, 2] *= gain;

			var filterValue = new FilterCoefficient();

			filterValue.Numerator = a;
			filterValue.Denominator = b;
			filterValue.Sections = sections;
			filterValue.Order = order;

			return filterValue;

		}

		/// <summary>
		/// バタワースローパスフィルタのデジタルフィルタ係数算出
		/// </summary>
		/// <param name="order">フィルタ次数</param>
		/// <param name="Wd">フィルタの正規化カットオフ周波数または中心周波数（ナイキスト周波数で正規化）</param>
		/// <returns>FilterCoefficient 双一次変換されたデジタルフィルタの係数</returns>
		public static FilterCoefficient ButterworthLowPassFilter(int order, double Wd)
		{
			var coeff = ButterworthLowHighFilter(order, Wd, FilterType.Low);
			return coeff;

		}

		/// <summary>
		/// バタワースハイパスフィルタのデジタルフィルタ係数算出
		/// </summary>
		/// <param name="order">フィルタ次数</param>
		/// <param name="Wd">フィルタの正規化カットオフ周波数または中心周波数（ナイキスト周波数で正規化）</param>
		/// <returns>FilterCoefficient 双一次変換されたデジタルフィルタの係数</returns>
		public static FilterCoefficient ButterworthHighPassFilter(int order, double Wd)
		{
			var coeff = ButterworthLowHighFilter(order, Wd, FilterType.High);
			return coeff;
		}

		/// <summary>
		/// バタワースローパス／ハイパスフィルタのデジタルフィルタ係数算出
		/// ButterworthLowPassFilter, ButterworthHighPassFilterから呼び出し
		/// </summary>
		/// <param name="order">フィルタ次数</param>
		/// <param name="Wd">フィルタの正規化カットオフ周波数または中心周波数（ナイキスト周波数で正規化）</param>
		/// <param name="filterType">フィルタの種類 FilterTypeで選択</param>
		/// <returns>FilterCoefficient 双一次変換されたデジタルフィルタの係数</returns>
		private static FilterCoefficient ButterworthLowHighFilter(int order, double Wd, FilterType filterType)
		{
			// 無次元カットオフ周波数(デジタル、設計値）のプリワーピング（設計値に合わせ、アナログフィルタの周波数を再計算）
			double Wc = 2 / Math.PI * Math.Tan(Math.PI / 2 * Wd);

			//フィルタの次数の偶数奇数判定
			bool odd = order % 2 != 0;

			//フィルタの段数
			int sections = (int)((order + 1) / 2);

			// フィルタの段数分、係数配列を初期化
			var a = new double[sections, 3];
			var b = new double[sections, 3];
			var aAnalog = new double[sections, 3];
			var bAnalog = new double[sections, 3];

			// 正規化バタワース多項式の極の算出
			var pk = NormalizedButterworthPoles(order);

			if (odd)
			{
				// 奇数の時

				// アナログフィルタの係数算出（a2s^2+a1s+a0) / (b2s^2+b1s+b0)
				// １段目（１次）
				aAnalog[0, 0] = 1;
				aAnalog[0, 1] = 0;
				aAnalog[0, 2] = 0;

				bAnalog[0, 0] = 1;
				bAnalog[0, 1] = 1;
				bAnalog[0, 2] = 0;

				// ２段目以降（２次）
				for (var k = 1; k < sections; k++)
				{
					aAnalog[k, 0] = 1;
					aAnalog[k, 1] = 0;
					aAnalog[k, 2] = 0;

					bAnalog[k, 0] = 1;
					bAnalog[k, 1] = 2 * Math.Cos(pk[k]);
					bAnalog[k, 2] = 1;
				}

				//ハイパスフィルタのとき、係数の順序を反転する
				if (filterType == FilterType.High)
				{
					var aBuffer = new double[aAnalog.GetLength(0), aAnalog.GetLength(1)];
					var bBuffer = new double[aAnalog.GetLength(0), aAnalog.GetLength(1)];

					aBuffer[0, 0] = aAnalog[0, 1];
					aBuffer[0, 1] = aAnalog[0, 0];

					bBuffer[0, 0] = bAnalog[0, 1];
					bBuffer[0, 1] = bAnalog[0, 0];

					for (var k = 1; k < sections; k++)
					{
						for (var j = 0; j < 3; j++)
						{
							aBuffer[k, j] = aAnalog[k, 2 - j];
							bBuffer[k, j] = bAnalog[k, 2 - j];
						}
					}
					aAnalog = aBuffer;
					bAnalog = bBuffer;
				}

			}
			else
			{
				// 偶数

				// アナログフィルタの係数算出
				for (var k = 0; k < sections; k++)
				{
					aAnalog[k, 0] = 1;
					aAnalog[k, 1] = 0;
					aAnalog[k, 2] = 0;

					bAnalog[k, 0] = 1;
					bAnalog[k, 1] = 2 * Math.Cos(pk[k]);
					bAnalog[k, 2] = 1;
				}

				//ハイパスフィルタのとき、係数の順序を反転する
				if (filterType == FilterType.High)
				{
					var aBuffer = new double[aAnalog.GetLength(0), aAnalog.GetLength(1)];
					var bBuffer = new double[aAnalog.GetLength(0), aAnalog.GetLength(1)];

					for (var k = 0; k < sections; k++)
					{
						for (var j = 0; j < 3; j++)
						{
							aBuffer[k, j] = aAnalog[k, 2 - j];
							bBuffer[k, j] = bAnalog[k, 2 - j];
						}
					}
					aAnalog = aBuffer;
					bAnalog = bBuffer;
				}
			}

			// 双一次変換
			var filter = BilinearTransform(order, Wc, aAnalog, bAnalog, filterType);


			return filter;
		}


		/// <summary>
		/// バタワースハイパスストップフィルタのデジタルフィルタ係数算出
		/// </summary>
		/// <param name="order">フィルタ次数</param>
		/// <param name="Wd">フィルタの正規化カットオフ周波数または中心周波数（ナイキスト周波数で正規化）</param>
		/// <param name="Bd">正規化バンド幅</param>
		/// <returns>FilterCoefficient 双一次変換されたデジタルフィルタの係数</returns>
		public static FilterCoefficient ButterworthBandStopFilter(int order, double Wd, double Bd)
		{
			var coeff = ButterworthBandFilter(order, Wd, Bd, FilterType.BandStop);
			return coeff;
		}


		/// <summary>
		/// バタワースハイパスストップフィルタのデジタルフィルタ係数算出
		/// </summary>
		/// <param name="order">フィルタ次数</param>
		/// <param name="Wd">フィルタの正規化カットオフ周波数または中心周波数（ナイキスト周波数で正規化）</param>
		/// <param name="Bd">正規化バンド幅</param>
		/// <param name="param">引数の種類、中心周波数か、上下端カットオフ周波数指定かを選択</param>
		/// <returns>FilterCoefficient 双一次変換されたデジタルフィルタの係数</returns>
		public static FilterCoefficient ButterworthBandStopFilter(int order, double low, double high, FilterParameterSelection param)
		{
			FilterCoefficient coeff;

			if (param == FilterParameterSelection.BandwidthAndCenterFrequency)
			{
				// 中心周波数指定となっていた場合
				double centerFrequency = low;
				double bandwidth = high;
				coeff = ButterworthBandStopFilter(order, centerFrequency, bandwidth);
			}
			else
			{
				// プリワーピング
				double lowAnalog = 2 / Math.PI * Math.Tan(Math.PI / 2 * low);
				double highAnalog = 2 / Math.PI * Math.Tan(Math.PI / 2 * high);

				// アナログ領域の中心周波数算出
				double Wa = Math.Sqrt(lowAnalog * highAnalog);
				//アナログの式に合わせて

				//アナログ領域のバンド幅
				//double Ba = highAnalog - lowAnalog;

				//デジタル領域に再変換
				double Wd = 2 / Math.PI * Math.Atan(Math.PI / 2 * Wa);
				double Bd = high - low;

				// フィルタ関数の呼び出し
				coeff = ButterworthBandFilter(order, Wd, Bd, FilterType.BandStop);

			}
			return coeff;
		}

		/// <summary>
		/// バタワースハイパスパスフィルタのデジタルフィルタ係数算出
		/// </summary>
		/// <param name="order">フィルタ次数</param>
		/// <param name="Wd">フィルタの正規化カットオフ周波数または中心周波数（ナイキスト周波数で正規化）</param>
		/// <param name="Bd">正規化バンド幅</param>
		/// <returns>FilterCoefficient 双一次変換されたデジタルフィルタの係数</returns>
		public static FilterCoefficient ButterworthBandPassFilter(int order, double Wd, double Bd)
		{
			var coeff = ButterworthBandFilter(order, Wd, Bd, FilterType.BandPass);
			return coeff;
		}


		/// <summary>
		/// バタワースハイパスパスフィルタのデジタルフィルタ係数算出
		/// </summary>
		/// <param name="order">フィルタ次数</param>
		/// <param name="Wd">フィルタの正規化カットオフ周波数または中心周波数（ナイキスト周波数で正規化）</param>
		/// <param name="Bd">正規化バンド幅</param>
		/// <param name="param">引数の種類、中心周波数か、上下端カットオフ周波数指定かを選択</param>
		/// <returns>FilterCoefficient 双一次変換されたデジタルフィルタの係数</returns>
		public static FilterCoefficient ButterworthBandPassFilter(int order, double low, double high, FilterParameterSelection param)
		{
			FilterCoefficient coeff;

			if (param == FilterParameterSelection.BandwidthAndCenterFrequency)
			{
				// 中心周波数指定となっていた場合
				double centerFrequency = low;
				double bandwidth = high;
				coeff = ButterworthBandPassFilter(order, centerFrequency, bandwidth);
			}
			else
			{
				// プリワーピング
				double lowAnalog = 2 / Math.PI * Math.Tan(Math.PI / 2 * low);
				double highAnalog = 2 / Math.PI * Math.Tan(Math.PI / 2 * high);

				// アナログ領域の中心周波数算出
				double Wa = Math.Sqrt(lowAnalog * highAnalog);
				//アナログの式に合わせて

				//アナログ領域のバンド幅
				//double Ba = highAnalog - lowAnalog;

				//デジタル領域に再変換
				double Wd = 2 / Math.PI * Math.Atan(Math.PI / 2 * Wa);
				double Bd = high - low;

				// フィルタ関数の呼び出し
				coeff = ButterworthBandFilter(order, Wd, Bd, FilterType.BandPass);

			}
			return coeff;
		}


		/// <summary>
		/// バタワースバンドパス／バンドストップフィルタのデジタルフィルタ係数算出
		/// ButterworthBandStopFilter, ButterworthBandPassFilterから呼び出し
		/// </summary>
		/// <param name="order">フィルタ次数</param>
		/// <param name="Wd">フィルタの正規化カットオフ周波数または中心周波数（ナイキスト周波数で正規化）</param>
		/// <param name="Bd">バンド幅</param>
		/// <param name="filterType">フィルタの種類 FilterTypeで選択</param>
		/// <returns>FilterCoefficient 双一次変換されたデジタルフィルタの係数</returns>
		private static FilterCoefficient ButterworthBandFilter(int order, double Wd, double Bd, FilterType filterType)
		{
			// 無次元中心周波数(デジタル、設計値）のプリワーピング（設計値に合わせ、アナログフィルタの周波数を再計算）
			double Wa = 2 / Math.PI * Math.Tan(Math.PI / 2 * Wd);

			// 無次元バンド幅のプリワーピング
			double Ba = (1 + Math.Pow(Math.PI,2) / 4 * Wa) * 2 / Math.PI * Math.Tan(Math.PI / 2 * Bd);

			// Q値のプリワーピング
			double Qa = Wa / Ba;

			// 正規化バタワース多項式にカットオフ周波数を反映
			// 極の計算

			//フィルタの次数の偶数奇数判定
			bool odd = order % 2 != 0;

			//2次フィルタの段数
			int sections = order;

			// 正規化バタワース多項式の極の算出
			var pk = NormalizedButterworthPoles(order);

			// フィルタの段数分、係数配列を初期化
			var a = new double[sections, 3];
			var b = new double[sections, 3];
			var aAnalog = new double[sections, 3];
			var bAnalog = new double[sections, 3];

			//バンドストップフィルタへ変換
			//極の角度η,極の半径aを求める
			double[] ceta = new double[pk.Length];
			double[] ap = new double[pk.Length];
			for (var k = 0; k < pk.Length; k++)
			{
				ceta[k] = 1 / 2.0 / Math.Sqrt(2) * Math.Sqrt(1 / Math.Pow(Qa, 2) + 4)
							* Math.Sqrt(1 - Math.Sqrt(1 - Math.Pow(Math.Cos(pk[k]), 2) * Math.Pow(4 * Qa / (1 + 4 * Math.Pow(Qa, 2)), 2)));

				ap[k] = 1 / 2.0 / Math.Sqrt(2) * Math.Sqrt(1 / Math.Pow(Qa, 2) + 4)
							* (Math.Sqrt(1 + Math.Sqrt(1 - Math.Pow(Math.Cos(pk[k]), 2) * Math.Pow(4 * Qa / (1 + 4 * Math.Pow(Qa, 2)), 2)))
								+ Math.Sqrt((1 - 4 * Math.Pow(Qa, 2)) / (1 + 4 * Math.Pow(Qa, 2)) + Math.Sqrt(1 - Math.Pow(Math.Cos(pk[k]), 2) * Math.Pow(4 * Qa / (1 + 4 * Math.Pow(Qa, 2)), 2)))
								);
			}

			// アナログフィルタの係数算出（a0s^2+a1s+a2) / (b0s^2+b1s+b2)
			// 奇数の最初の極以外は、１つの極から２つの対となるセクションが生成される
			int section = 0;
			for (var k = 0; k < pk.Length; k++)
			{
				if(filterType == FilterType.BandStop)
				{
					aAnalog[section, 0] = 1;
					aAnalog[section, 1] = 0;
					aAnalog[section, 2] = 1;
				}
				else
				{
					aAnalog[section, 0] = 0;
					aAnalog[section, 1] = 1/Qa;
					aAnalog[section, 2] = 0;

				}

				bAnalog[section, 0] = 1;
				bAnalog[section, 1] = 2 * ap[k] * ceta[k];
				bAnalog[section, 2] = Math.Pow(ap[k], 2);
				section++;

				// 奇数の最初の極以外の対となるセクション
				if (k > 0 || !odd) {
					if (filterType == FilterType.BandStop)
					{
						aAnalog[section, 0] = 1;
						aAnalog[section, 1] = 0;
						aAnalog[section, 2] = 1;
					}
					else
					{
						aAnalog[section, 0] = 0;
						aAnalog[section, 1] = 1 / Qa;
						aAnalog[section, 2] = 0;

					}

					bAnalog[section, 0] = 1;
					bAnalog[section, 1] = 2 / ap[k] * ceta[k];
					bAnalog[section, 2] = 1 / Math.Pow(ap[k], 2);
					section++;
				}
			}

			// 双一次変換の変換係数
			var filter = BilinearTransform(order, Wa, aAnalog, bAnalog,FilterType.BandStop);

			return filter;

		}

	}

}
