using MySql.Data.MySqlClient;
using Renci.SshNet.Messages;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace HFT_Order
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        List<Symbol> symbollist;
        string connection, logt;
        MySqlConnection conn;
        DataTable dt;

        public MainWindow()
        {
            InitializeComponent();
            symbollist = new List<Symbol>();
            connection = ConfigurationManager.ConnectionStrings["hft_prod"].ConnectionString;
            conn = new MySqlConnection(connection);
            if (connect_db())
                select_hft();
        }

        bool connect_db()
        {
            bool result = false;
            if (conn.State == System.Data.ConnectionState.Open)
            {
                conn.Close();
                update_log("Connection closed");                             
            }
                
            try
            {               
                conn.Open();
                DB_Connection.Content = "DB Connection: OK";
                update_log("Connected to DB");
                result = true;
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
                DB_Connection.Content = "DB Connection: FAILED";
                result = false;
            }
            return result;
        }

        bool select_hft()
        {
            bool result = false;            
            MySqlCommand comm;

            if (conn.State == System.Data.ConnectionState.Open)
            {
                try
                {
                    comm = new MySqlCommand("select * from firstorder", conn);
                    MySqlDataAdapter returned = new MySqlDataAdapter("select * from firstorder", conn);
                    dt = new DataTable();
                    returned.Fill(dt);
                    data_grid.ItemsSource = dt.DefaultView;

                    update_log("Latest order view data pulled.");                    
                }
                catch (Exception ex)
                {
                    MessageBox.Show(ex.Message);
                }
            }

            return result;
        }

        bool trunc_hft()
        {
            bool result = false;
            MySqlCommand comm;

            if (conn.State == System.Data.ConnectionState.Open)
            {
                try
                {
                    comm = new MySqlCommand("truncate table firstorder", conn);
                    comm.ExecuteNonQuery();
                    
                    update_log("Table \"First Order\" truncated.");
                }
                catch (Exception ex)
                {
                    update_log(ex.Message);
                }
            }
            return result;
        }
        
        private void db_check_btn_Click(object sender, RoutedEventArgs e)
        {
            connect_db();
        }

        void delete_order(object sender, RoutedEventArgs e)
        {
            try
            {
                DataRowView row = (DataRowView)data_grid.SelectedItem;
                update_log(row[0] + " " + row[1] + " " + row[2] + " " + row[3] + "order EXCLUDED from orders!");
                dt.Rows.Remove(row.Row);
            }
            catch (Exception) { }
                     
        }

        List<Symbol> get_recent_data()
        {
            List<Symbol> new_orders = new List<Symbol>();
            if (dt.Rows.Count > 0)
                foreach (DataRow item in dt.Rows)
                {
                    new_orders.Add(new Symbol
                    {
                        OrderBookId = Convert.ToInt32(item[0]),
                        OrderPrice = Convert.ToInt32(item[1]),
                        OrderLot = Convert.ToInt32(item[2]),
                        CustomerNo = Convert.ToInt32(item[3])
                    });
                }
            else { 
                MessageBox.Show("No order information in table!");                
            }
            return new_orders;
        }

        bool send_orders(List<Symbol> sym_list) {
            bool result = false;
            trunc_hft();
            string q_text = "INSERT INTO firstorder (OrderBookId, OrderPrice, OrderLot, CustomerNo) VALUES (@orderbookid, @orderprice, @orderlot, @customerno)";
            foreach (Symbol sym in sym_list)
            {
                MySqlCommand comm = new MySqlCommand(q_text, conn);           
                comm.Parameters.AddWithValue("@orderbookid", sym.OrderBookId);
                comm.Parameters.AddWithValue("@orderprice", sym.OrderPrice);
                comm.Parameters.AddWithValue("@orderlot", sym.OrderLot);
                comm.Parameters.AddWithValue("@customerno", sym.CustomerNo);
                comm.ExecuteNonQuery();
                comm.Dispose();
                update_log("ORDER : "+sym.OrderBookId + " - " + sym.OrderPrice + " - " + sym.OrderLot + " - " + sym.CustomerNo + " is scheduled");
            }
            return result;
        }

        private void chck_instance_Click(object sender, RoutedEventArgs e)
        {
            if (conn.State == System.Data.ConnectionState.Open)
                select_hft();
            else MessageBox.Show("Connection error");
        }

        private void env_change_click(object sender, RoutedEventArgs e)
        {
            if (env_label.Content.ToString().Contains("PROD"))
            {
                if (conn.State == ConnectionState.Open)
                    conn.Close();
                connection = ConfigurationManager.ConnectionStrings["hft_test"].ConnectionString;
                conn = new MySqlConnection(connection);
                if(connect_db())
                {
                    select_hft();
                    env_label.Content = "Current Environment : TEST";
                    update_log("Environment changed to TEST");
                }
            }
            else
            {
                if (conn.State == ConnectionState.Open)
                    conn.Close();
                connection = ConfigurationManager.ConnectionStrings["hft_prod"].ConnectionString;
                conn = new MySqlConnection(connection);

                if (connect_db())
                {
                    select_hft();
                    env_label.Content = "Current Environment : PROD";
                    update_log("Environment changed to PROD");
                }
            }
        }

        private void Senddata_btn_Click(object sender, RoutedEventArgs e)
        {
            string orders;
            List<Symbol> n_orders = get_recent_data();
            if(n_orders != null)
            {
                MessageBoxResult result = MessageBox.Show("Emirleriniz sunucuya iletilecektir. Onaylıyor musunuz?", "Onay", MessageBoxButton.OKCancel);
                switch (result)
                {
                    case MessageBoxResult.OK:
                        if (send_orders(n_orders))
                            if (select_hft())
                                update_log("New table instance pulled.");
                            else
                                update_log("Error querying table 'firstorder'");

                        break;
                    case MessageBoxResult.Cancel:
                        orders = "*****************************************\n";
                        foreach (Symbol sym in n_orders)
                        {
                            orders += sym.OrderBookId + " " + sym.OrderPrice + " " + sym.OrderLot + " " + sym.CustomerNo+ "\n";
                        }
                        orders += "Emir gönderimi iptal edildi.\n*****************************************\n";
                        update_log(orders);                      
                        
                        return;                        
                }  
            }                
            MessageBox.Show("Order(s) sent successfully!", "Success...");
            update_log("Order(s) sent successfully!");
        }

        void update_log(string message)
        {
            logt = message + " - " + DateTime.Now + "\n" + log_t.Text;
            log_t.Text = logt;
        }
    }
}
