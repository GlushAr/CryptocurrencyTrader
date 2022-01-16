﻿using System;
using System.Data.SqlClient;
using System.Collections.Generic;
using Npgsql;

namespace bot_fedot {
	// database : postgres
	static class SqlConn {
		public static string connection_string { get; private set; }

		public static void initConnStr(string connection_string_) {
			connection_string = connection_string_;
		}

		public static void changeLastPurchase(int id, float last_purchase_price) {
			using (NpgsqlConnection connection = new NpgsqlConnection(connection_string)) {
				connection.Open();

				string queryString = $"UPDATE trades SET trade_ttate_is_sell = 'true', last_purchase_price = {last_purchase_price}" +
									 $"WHERE id_trade = {id}";

				NpgsqlCommand comm = new NpgsqlCommand(queryString, connection);
				comm.ExecuteNonQuery();
				connection.Close();
			}
		}

		public static void changeLastSelling(int id, float last_selling_price) {
			using (NpgsqlConnection connection = new NpgsqlConnection(connection_string)) {
				connection.Open();

				string queryString = $"UPDATE Trades SET Trade_State_Is_Sell = 'False', Last_Selling_Price = {last_selling_price}" +
									 $"WHERE Id_Trade = {id}";

				NpgsqlCommand comm = new NpgsqlCommand(queryString, connection);
				comm.ExecuteNonQuery();
				connection.Close();
			}
		}

		public static void getListOfTrades(List<TradeItems> trade) {
			int id;
			int id_owner;
			string pair;
			float quantity;
			bool trade_state_is_sell;

			float last_purchase_price;
			float last_selling_price;

			float min_profit_percent;
			float drop_percent_after_peak;

			float min_rollback_percent;
			float growth_percent_after_bottom;

			using (NpgsqlConnection connection = new NpgsqlConnection(connection_string)) {
				connection.Open();

				string queryString = "SELECT * FROM Trades";

				NpgsqlCommand comm = new NpgsqlCommand(queryString, connection);
				NpgsqlDataReader reader = comm.ExecuteReader();

				while (reader.Read()) {
					short i = 0;
					id = (int)reader.GetValue(i++);
					id_owner = (int)reader.GetValue(i++);
					pair = ((string)reader.GetValue(i++)).Trim();
					quantity = (float)((double)reader.GetValue(i++));
					trade_state_is_sell = (bool)reader.GetValue(i++);
					last_purchase_price = (float)((double)reader.GetValue(i++));
					last_selling_price = (float)((double)reader.GetValue(i++));
					min_profit_percent = (float)((double)reader.GetValue(i++));
					drop_percent_after_peak = (float)((double)reader.GetValue(i++));
					min_rollback_percent = (float)((double)reader.GetValue(i++));
					growth_percent_after_bottom = (float)((double)reader.GetValue(i));

					trade.Add(new TradeItems(id, id_owner, pair, quantity, trade_state_is_sell, last_purchase_price,
												last_selling_price, min_profit_percent, -drop_percent_after_peak,
												-min_rollback_percent, growth_percent_after_bottom));
				}
				reader.Close();
				connection.Close();
			}
		}

		public static void errorPrint(string error_msg, TimeSpan lasting) {
			using (NpgsqlConnection connection = new NpgsqlConnection(connection_string)) {
				connection.Open();

				string queryString = "INSERT INTO errors (date, lasting, content)" +
									$"VALUES('{DateTime.Now.ToString("yyyyMMdd HH:mm:ss")}', '{lasting}', '{error_msg.ToString()}')";

				NpgsqlCommand comm = new NpgsqlCommand(queryString, connection);
				comm.ExecuteNonQuery();
				connection.Close();
			}
		}

		public static void printLog(Currency twin, TradeItems trade) {
			using (NpgsqlConnection connection = new NpgsqlConnection(connection_string)) {
				connection.Open();

				string state = trade.trade_state_is_sell ? "BOUGHT" : "SOLD";

				float percent = trade.trade_state_is_sell ? ((twin.sell_price - trade.last_selling_price) / trade.last_selling_price) * 100
														  : ((twin.buy_price - trade.last_purchase_price) / trade.last_purchase_price) * 100;

				string queryString = "INSERT INTO TradesLogs (Id_Trade, Id_Owner, Sold_Bought, Date, Percents)" +
									$"VALUES({trade.id}, {trade.id_owner}, '{state}', '{DateTime.Now.ToString("yyyyMMdd HH:mm:ss")}', {percent})";

				NpgsqlCommand comm = new NpgsqlCommand(queryString, connection);
				comm.ExecuteNonQuery();
				connection.Close();
			}
		}
	}
}
