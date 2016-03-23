using System;
using System.Collections.Generic;

namespace Filter
{
    class Program
    {


		static void Main(string[] args)
        {

			double frequency = 1000;
			double cutoff = 20;
			double Wd = cutoff / frequency * 2;

			double bandwidth = 5;
			double Bd = bandwidth / frequency * 2;

			int order = 5;

			//ドロップされたファイル名を取得
			string[] files = System.Environment.GetCommandLineArgs();

			// フィルタ係数の算出
			var coeffLow = ButterworthFilterDesign.ButterworthLowPassFilter(order, Wd);
			var coeffHigh = ButterworthFilterDesign.ButterworthHighPassFilter(order, Wd);
			var coeffBandStop = ButterworthFilterDesign.ButterworthBandStopFilter(order, Wd, Bd);
			var coeffBandPass = ButterworthFilterDesign.ButterworthBandPassFilter(order, Wd, Bd);

			// 出力データの保存用リスト
			var yLow = new List<double>();
			var yHigh = new List<double>();
			var yBandStop = new List<double>();
			var yBandPass = new List<double>();
			
			//フィルタクラスのインスタンスを生成
			var filterLow = new Filtering(coeffLow);
			var filterHigh = new Filtering(coeffHigh);
			var filterBandStop = new Filtering(coeffBandStop);
			var filterBandPass = new Filtering(coeffBandPass);

			try
			{
				//ドロップされたCSVファイルの読み込み
				var data = ReadCsv(files[1]);

				//フィルタの実行
				foreach (var datum in data)
				{
					yLow.Add(filterLow.Filter(datum));
					yHigh.Add(filterHigh.Filter(datum));
					yBandStop.Add(filterBandStop.Filter(datum));
					yBandPass.Add(filterBandPass.Filter(datum));
				}

				// 出力用のファイル名を生成
				var path = System.IO.Path.GetDirectoryName(files[1]);
				var fileNameWithoutExtension = System.IO.Path.GetFileNameWithoutExtension(files[1]);
				var fileExtension = System.IO.Path.GetExtension(files[1]);
				string fname = path +"\\"+ fileNameWithoutExtension + "_filtered" + fileExtension;

				//ファイルを出力
				WriteCsv(fname, data, yLow, yHigh, yBandStop, yBandPass);

			}
			catch (System.Exception e)
			{
				// ファイルを開くのに失敗したときエラーメッセージを表示
				System.Console.WriteLine(e.Message);
			}

		}

		static List<double> ReadCsv(string fname)
		{

			try
			{
				List<double> dataList = new List<double>();
				// csvファイルを開く
//				string fname = System.Environment.GetFolderPath(Environment.SpecialFolder.DesktopDirectory) + ".\\chirp.csv";

				using (var sr = new System.IO.StreamReader(fname))
				{
					// ストリームの末尾まで繰り返す
					while (!sr.EndOfStream)
					{
						// ファイルから一行読み込む
						var line = sr.ReadLine();
						// 読み込んだ一行をカンマ毎に分けて配列に格納する
						var values = line.Split(',');
						// 出力する
						foreach (var value in values)
						{
							dataList.Add(double.Parse(value));
						}
					}
				}
				return dataList;
			}
			catch (System.Exception e)
			{
				// ファイルを開くのに失敗したとき
				System.Console.WriteLine(e.Message);
				return null;
			}
		}

		private static void WriteCsv(string fname, List<double> data1, List<double> data2, List<double> data3, List<double> data4, List<double> data5)
		{
			try
			{
				// appendをtrueにすると，既存のファイルに追記
				// falseにすると，ファイルを新規作成する
				var append = false;

				using (var sw = new System.IO.StreamWriter(fname, append))
				{
					for (int cnt = 0; cnt < data1.Count; cnt++)
					{
						sw.WriteLine("{0}\t{1}\t{2}\t{3}\t{4}", data1[cnt], data2[cnt], data3[cnt], data4[cnt], data5[cnt]);
					}
				}
			}
			catch (System.Exception e)
			{
				// ファイルを開くのに失敗したときエラーメッセージを表示
				System.Console.WriteLine(e.Message);
			}
		}


	}
}
