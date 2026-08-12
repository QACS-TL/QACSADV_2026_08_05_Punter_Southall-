using System;
using System.Data;
// Use this for .NET Core / .NET 5+
// Install using NuGet
using Microsoft.Data.SqlClient; // Use this for .NET Core / .NET 5+

namespace NorthwindAdoDemo
{
    class ADONET_Demo
    {
        // Adjust server name / auth as needed
        private static readonly string ConnectionString =
            "Server=(local);Database=Northwind;Integrated Security=True;TrustServerCertificate=True;Encrypt=False";

        static void Main(string[] args)
        {
            BasicQueryWithDataReader();
            Console.WriteLine();

            ParameterizedQuery(10);
            Console.WriteLine();

            InsertUpdateDeleteDemo();
            Console.WriteLine();

            FillDataSetWithAdapter();
            Console.WriteLine();

            ExecuteScalarDemo();
            Console.WriteLine();

            TransactionDemo();

            Console.WriteLine("\nDone. Press any key to exit.");
            Console.ReadKey();
        }

        // 1. Basic connection + command + reader
        static void BasicQueryWithDataReader()
        {
            Console.WriteLine("=== Basic Query (SqlDataReader) ===");

            const string sql = "SELECT CustomerID, CompanyName, ContactName, Country FROM Customers ORDER BY CompanyName";

            using (SqlConnection connection = new SqlConnection(ConnectionString))
            using (SqlCommand command = new SqlCommand(sql, connection))
            {
                try
                {
                    connection.Open();

                    using (SqlDataReader reader = command.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            string customerId = reader["CustomerID"].ToString();
                            string companyName = reader["CompanyName"].ToString();
                            string contactName = reader["ContactName"] as string ?? "(none)";
                            string country = reader["Country"] as string ?? "(unknown)";

                            Console.WriteLine($"{customerId} | {companyName} | {contactName} | {country}");
                        }
                    }
                }
                catch (SqlException ex)
                {
                    Console.WriteLine("Database error: " + ex.Message);
                }
            }
        }

        // 2. Parameterized query (always use parameters, never string-concat SQL!)
        static void ParameterizedQuery(int categoryId)
        {
            Console.WriteLine($"=== Products in Category {categoryId} (Parameterized) ===");

            const string sql = @"SELECT ProductID, ProductName, UnitPrice, UnitsInStock
                                  FROM Products
                                  WHERE CategoryID = @CategoryID
                                  ORDER BY ProductName";

            using (SqlConnection connection = new SqlConnection(ConnectionString))
            using (SqlCommand command = new SqlCommand(sql, connection))
            {
                command.Parameters.Add(new SqlParameter("@CategoryID", SqlDbType.Int) { Value = categoryId });

                try
                {
                    connection.Open();
                    using (SqlDataReader reader = command.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            int productId = reader.GetInt32(reader.GetOrdinal("ProductID"));
                            string productName = reader.GetString(reader.GetOrdinal("ProductName"));
                            decimal price = reader.IsDBNull(reader.GetOrdinal("UnitPrice"))
                                ? 0 : reader.GetDecimal(reader.GetOrdinal("UnitPrice"));
                            short stock = reader.IsDBNull(reader.GetOrdinal("UnitsInStock"))
                                ? (short)0 : reader.GetInt16(reader.GetOrdinal("UnitsInStock"));

                            Console.WriteLine($"#{productId} {productName} - ${price:F2} ({stock} in stock)");
                        }
                    }
                }
                catch (SqlException ex)
                {
                    Console.WriteLine("Database error: " + ex.Message);
                }
            }
        }

        // 3. Insert / Update / Delete using ExecuteNonQuery
        static void InsertUpdateDeleteDemo()
        {
            Console.WriteLine("=== Insert / Update / Delete (ExecuteNonQuery) ===");

            using (SqlConnection connection = new SqlConnection(ConnectionString))
            {
                connection.Open();

                // INSERT
                const string insertSql = @"INSERT INTO Shippers (CompanyName, Phone)
                                            VALUES (@CompanyName, @Phone);
                                            SELECT SCOPE_IDENTITY();"; // returns new identity value

                int newId;
                using (SqlCommand insertCmd = new SqlCommand(insertSql, connection))
                {
                    insertCmd.Parameters.AddWithValue("@CompanyName", "Speedy Freight Co.");
                    insertCmd.Parameters.AddWithValue("@Phone", "(503) 555-0199");

                    object result = insertCmd.ExecuteScalar();
                    newId = Convert.ToInt32(result);
                    Console.WriteLine($"Inserted new Shipper with ID: {newId}");
                }

                // UPDATE
                const string updateSql = "UPDATE Shippers SET Phone = @Phone WHERE ShipperID = @ShipperID";
                using (SqlCommand updateCmd = new SqlCommand(updateSql, connection))
                {
                    updateCmd.Parameters.AddWithValue("@Phone", "(503) 555-0200");
                    updateCmd.Parameters.AddWithValue("@ShipperID", newId);

                    int rowsAffected = updateCmd.ExecuteNonQuery();
                    Console.WriteLine($"Updated {rowsAffected} row(s).");
                }

                // DELETE (cleanup)
                const string deleteSql = "DELETE FROM Shippers WHERE ShipperID = @ShipperID";
                using (SqlCommand deleteCmd = new SqlCommand(deleteSql, connection))
                {
                    deleteCmd.Parameters.AddWithValue("@ShipperID", newId);
                    int rowsAffected = deleteCmd.ExecuteNonQuery();
                    Console.WriteLine($"Deleted {rowsAffected} row(s).");
                }
            }
        }

        // 4. SqlDataAdapter + DataSet (disconnected data access)
        static void FillDataSetWithAdapter()
        {
            Console.WriteLine("=== SqlDataAdapter + DataSet ===");

            const string sql = "SELECT TOP 5 OrderID, CustomerID, OrderDate, Freight FROM Orders ORDER BY OrderDate DESC";

            using (SqlConnection connection = new SqlConnection(ConnectionString))
            using (SqlDataAdapter adapter = new SqlDataAdapter(sql, connection))
            {
                DataSet dataSet = new DataSet();

                try
                {
                    adapter.Fill(dataSet, "Orders");

                    DataTable table = dataSet.Tables["Orders"];
                    foreach (DataRow row in table.Rows)
                    {
                        Console.WriteLine($"Order {row["OrderID"]} | Customer {row["CustomerID"]} | " +
                                          $"{Convert.ToDateTime(row["OrderDate"]):yyyy-MM-dd} | Freight: {row["Freight"]}");
                    }
                }
                catch (SqlException ex)
                {
                    Console.WriteLine("Database error: " + ex.Message);
                }
            }
        }

        // 5. ExecuteScalar for single-value results (e.g., counts, aggregates)
        static void ExecuteScalarDemo()
        {
            Console.WriteLine("=== ExecuteScalar (Aggregate) ===");

            const string sql = "SELECT COUNT(*) FROM Customers WHERE Country = @Country";

            using (SqlConnection connection = new SqlConnection(ConnectionString))
            using (SqlCommand command = new SqlCommand(sql, connection))
            {
                command.Parameters.AddWithValue("@Country", "Germany");

                connection.Open();
                int count = (int)command.ExecuteScalar();
                Console.WriteLine($"Number of German customers: {count}");
            }
        }

        // 6. Transaction demo — commit/rollback grouped operations
        static void TransactionDemo()
        {
            Console.WriteLine("=== Transaction Demo ===");

            using (SqlConnection connection = new SqlConnection(ConnectionString))
            {
                connection.Open();
                SqlTransaction transaction = connection.BeginTransaction();

                try
                {
                    using (SqlCommand cmd1 = new SqlCommand(
                        "UPDATE Products SET UnitsInStock = UnitsInStock - 1 WHERE ProductID = @Id",
                        connection, transaction))
                    {
                        cmd1.Parameters.AddWithValue("@Id", 1);
                        cmd1.ExecuteNonQuery();
                    }

                    using (SqlCommand cmd2 = new SqlCommand(
                        "UPDATE Products SET UnitsInStock = UnitsInStock + 1 WHERE ProductID = @Id",
                        connection, transaction))
                    {
                        cmd2.Parameters.AddWithValue("@Id", 2);
                        cmd2.ExecuteNonQuery();
                    }

                    transaction.Commit();
                    Console.WriteLine("Transaction committed successfully.");
                }
                catch (Exception ex)
                {
                    transaction.Rollback();
                    Console.WriteLine("Transaction rolled back due to error: " + ex.Message);
                }
            }
        }
    }
}