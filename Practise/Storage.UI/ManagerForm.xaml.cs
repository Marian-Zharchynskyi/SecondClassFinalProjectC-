using Microsoft.EntityFrameworkCore;
using Microsoft.Win32;
using Newtonsoft.Json;
using OfficeOpenXml;
using Storage.Core.Context;
using Storage.Core.Entities;
using Storage.DataManager.Repositories;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;

namespace Storage.UI
{
    /// <summary>
    /// Interaction logic for ManagerForm.xaml
    /// </summary>
    public partial class ManagerForm : Window
    {
        private StorageContext dbContext;
        private Repository<Category> _categoryRepository;
        private Repository<Product> _productRepository;
        private Repository<OrderDetails> _orderDetailsRepository;
        private Repository<Order> _orderRepository;
        private Repository<TransactionType> _transactionTypeRepository;
        private Repository<Status> _statusRepository;

        public ManagerForm()
        {
            InitializeComponent();

            InitializeRepositories();
            Loaded += MainWindow_Loaded;

            UpdateProducts();

            ComboBoxOrders.ItemsSource = dbContext.Orders.ToList();

            ComboBoxOrders.SelectionChanged += ComboBoxOrders_SelectionChanged;

            dataGridStatus.SelectionChanged += dataGridStatus_SelectionChanged;

        }

        private async void MainWindow_Loaded(object sender, RoutedEventArgs e)
        {
 
            await LoadStatusesComboBox();
            await LoadAllProducts();
            await LoadAllCategories();
            await LoadAllOrdersHistory();
            await LoadProductsToCatalogAsync();
            await LoadCategoriesAsync();
            await LoadAllOrdersToCB();
        }

        private async Task LoadProductsToCatalogAsync()
        {
            using (var dbContext = new StorageContext())
            {
                LViewProduct.ItemsSource = await dbContext.Products.ToListAsync();
            }
        }

        private void dataGridStatus_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            Status selectedStatus = dataGridStatus.SelectedItem as Status;

            if (selectedStatus != null)
            {
                txtStatusName.Text = selectedStatus.Name;
            }
        }

        private async Task LoadCategoriesAsync()
        {
            using (var dbContext = new StorageContext())
            {
                var allCategories = await dbContext.Categories.ToListAsync();
                ComboCategory.ItemsSource = null;
                allCategories.Insert(0, new Category { Name = "All types" });
                ComboCategory.ItemsSource = allCategories;
                ComboCategory.SelectedIndex = 0;
            }
        }

        private void InitializeRepositories()
        {
            dbContext = new StorageContext();
            _categoryRepository = new Repository<Category>(dbContext);
            _productRepository = new Repository<Product>(dbContext);
            _orderDetailsRepository = new Repository<OrderDetails>(dbContext);
            _orderRepository = new Repository<Order>(dbContext);
            _transactionTypeRepository = new Repository<TransactionType>(dbContext);
            _statusRepository = new Repository<Status>(dbContext);
        }

        private void UpdateProducts()
        {
            var currentProducts = dbContext.Products.ToList();
            if (ComboCategory.SelectedIndex > 0)
            {
                currentProducts = currentProducts.Where(p => p.Category.Name == (ComboCategory.SelectedItem as Category).Name).ToList();
            }
            currentProducts = currentProducts.Where(p => p.Name.ToLower().Contains(TBoxSearch.Text.ToLower())).ToList();
            LViewProduct.ItemsSource = currentProducts.OrderBy(p => p.ProductQuantity).ToList();
        }

        private void TBoxSearch_TextChanged(object sender, EventArgs e)
        {
            UpdateProducts();
        }

        private void ComboCategory_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            UpdateProducts();
        }

        private async Task LoadAllProducts()
        {
            var productsWithCategories = await dbContext.Products.Include(p => p.Category).ToListAsync();
            var categories = await dbContext.Categories.ToListAsync();            

            if (productsWithCategories != null && productsWithCategories.Any())
            {
                dataGridProducts.ItemsSource = productsWithCategories;
            }
            else
            {
                MessageBox.Show("There aren't any data to display!");
            }

        }



        private async Task LoadAllCategories()
        {
            var categories = await _categoryRepository.GetAllAsync();

            if (categories != null && categories.Any())
            {
                dataGridCategories.ItemsSource = categories;
            }
            else
            {
                MessageBox.Show("There aren't any data to display!");
            }
        }


        private async Task LoadAllOrdersToCB()
        {
            var orders = await _orderRepository.GetAllAsync();
            ComboBoxOrders.ItemsSource = orders;
        }

        private async Task LoadAllOrderDetails()
        {
            var orderDetails = await _orderDetailsRepository.GetAllAsync();

            if (orderDetails != null && orderDetails.Any())
            {
                dataGridOrderDetails.ItemsSource = orderDetails;
            }
            else
            {
                MessageBox.Show("There aren't any data to display!");
            }
        }


        private async void CreateOrder_Click(object sender, RoutedEventArgs e)
        {
            if (DatePicker.SelectedDate == null)
            {
                MessageBox.Show("Please select a date for the order.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            DateTime orderDate = DatePicker.SelectedDate.Value;

            Status gatheringStatus = _statusRepository.GetQueryable().FirstOrDefault(s => s.Name == "Gathering at warehouse");

            if (gatheringStatus == null)
            {
                MessageBox.Show("Error: Status 'Gathering at warehouse' not found.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            Order newOrder = new Order
            {
                OrderDate = orderDate,
                Status = gatheringStatus
            };

            _orderRepository.Insert(newOrder);
            await _orderRepository.SaveChangesAsync(); 

            MessageBox.Show("Order created successfully.", "Success", MessageBoxButton.OK, MessageBoxImage.Information);

            await LoadAllOrdersToCB();

            await LoadAllOrdersHistory();
        }


        private async void SaveOrderSettings_Click(object sender, RoutedEventArgs e)
        {
            if (ComboBoxOrders.SelectedItem != null)
            {
                Order selectedOrder = ComboBoxOrders.SelectedItem as Order;

                Status selectedStatus = ComboBoxStatus.SelectedItem as Status;

                if (selectedStatus != null)
                {
                    if (selectedStatus.Name == "Delivered")
                    {
                        MessageBox.Show("Please press another button for the final status change.",
                                        "Warning", MessageBoxButton.OK, MessageBoxImage.Warning);
                    }
                    else
                    {
                        selectedOrder.Status = selectedStatus;
                        _orderRepository.Update(selectedOrder);
                        await _orderRepository.SaveChangesAsync();
                        await LoadAllOrderDetails();
                        await LoadAllOrdersHistory();

                        MessageBox.Show("Order status updated successfully.",
                                        "Success", MessageBoxButton.OK, MessageBoxImage.Information);
                    }
                }
                else
                {
                    MessageBox.Show("Please select a status for the order.",
                                    "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
            else
            {
                MessageBox.Show("Please select an order.",
                                "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }


        private void ComboBoxOrders_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (ComboBoxOrders.SelectedItem != null)
            {
                Order selectedOrder = ComboBoxOrders.SelectedItem as Order;


                var orderDetails = dbContext.OrderDetails
                    .Include(od => od.Order)
                    .Where(od => od.Order.Id == selectedOrder.Id)
                    .ToList();
                dataGridOrderDetails.ItemsSource = orderDetails;


                float totalOrderSum = orderDetails.Sum(od => od.Price);
                orderSum.Text = totalOrderSum.ToString();

            }
        }

        private async void AddOrderDetailToOrder_Click(object sender, RoutedEventArgs e)
        {
            if (ComboBoxOrders.SelectedItem == null)
            {
                MessageBox.Show("Please select an order first.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            if (!int.TryParse(QuantityToAddTextBox.Text, out int quantityToAdd))
            {
                MessageBox.Show("Please enter a valid quantity.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            Order selectedOrder = ComboBoxOrders.SelectedItem as Order;

            var selectedProduct = LViewProduct.SelectedItem as Product;

            if (selectedProduct == null)
            {
                MessageBox.Show("Please select a product first.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            if (selectedProduct.ProductQuantity < quantityToAdd)
            {
                MessageBox.Show("Insufficient product quantity.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            OrderDetails newOrderDetail = new OrderDetails
            {
                Order = selectedOrder,
                Product = selectedProduct,
                OrderProductQuantity = quantityToAdd,
                Price = selectedProduct.Price * quantityToAdd
            };

            _orderDetailsRepository.Insert(newOrderDetail);

            await _orderDetailsRepository.SaveChangesAsync();

            await LoadAllOrderDetails();

            MessageBox.Show("Product added to order successfully.", "Success", MessageBoxButton.OK, MessageBoxImage.Information);

            QuantityToAddTextBox.Text = "";
        }

        private async void RemoveOrderDetailFromOrder_Click(object sender, RoutedEventArgs e)
        {
            if (dataGridOrderDetails.SelectedItem == null)
            {
                MessageBox.Show("Please select an order detail to remove.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            OrderDetails selectedOrderDetail = dataGridOrderDetails.SelectedItem as OrderDetails;

            MessageBoxResult result = MessageBox.Show($"Are you sure you want to remove the selected order detail?", "Confirm Removal", MessageBoxButton.YesNo, MessageBoxImage.Question);

            if (result != MessageBoxResult.Yes)
                return;

            try
            {
                selectedOrderDetail.Order = null;

                await _orderDetailsRepository.SaveChangesAsync();

                MessageBox.Show("Order detail removed successfully.", "Success", MessageBoxButton.OK, MessageBoxImage.Information);

                await LoadAllOrderDetails();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"An error occurred while removing the order detail: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }


        private async void DeleteOrder_Click(object sender, RoutedEventArgs e)
        {
            if (ComboBoxOrders.SelectedItem == null)
            {
                MessageBox.Show("Please select an order to delete.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            Order selectedOrder = ComboBoxOrders.SelectedItem as Order;

            MessageBoxResult result = MessageBox.Show($"Are you sure you want to delete the order with ID {selectedOrder.Id}?", "Confirm Deletion", MessageBoxButton.YesNo, MessageBoxImage.Question);

            if (result != MessageBoxResult.Yes)
                return;

            try
            {
                _orderRepository.Delete(selectedOrder);
                await _orderRepository.SaveChangesAsync();

                MessageBox.Show("Order deleted successfully.", "Success", MessageBoxButton.OK, MessageBoxImage.Information);

                await LoadAllOrderDetails();
                await LoadAllOrdersHistory();
                await LoadAllOrdersToCB();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"An error occurred while deleting the order: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }



        private async Task LoadAllOrdersHistory()
        {
            var orders = await _orderRepository.GetAllAsync();
            dataGridOrdersHistory.ItemsSource = orders;

        }

        private async Task LoadAllTransactionTypes()
        {
            var transactionTypes = await _transactionTypeRepository.GetAllAsync();
            dataGridTransactionType.ItemsSource = transactionTypes;
        }

        private async Task LoadAllStatuses()
        {
            var statuses = await _statusRepository.GetAllAsync();
            dataGridStatus.ItemsSource = statuses;
        }

        private async Task LoadStatusesComboBox()
        {
            var statuses = await _statusRepository.GetAllAsync();

            ComboBoxStatus.ItemsSource = statuses;
        }

        private void Logout_Clicked(object sender, RoutedEventArgs e)
        {
            MessageBoxResult result = MessageBox.Show("Are you sure you want to log out?", "Logout", MessageBoxButton.YesNo, MessageBoxImage.Question);

            if (result == MessageBoxResult.Yes)
            {
                LoginForm loginForm = new LoginForm();
                loginForm.Show();
                this.Close();
            }
        }


        private async void OrderDelivered_Click(object sender, RoutedEventArgs e)
        {
            if (ComboBoxOrders.SelectedItem is not Order selectedOrder || ComboBoxStatus.SelectedItem is not Status selectedStatus || selectedStatus.Name != "Delivered")
            {
                MessageBox.Show("To complete the order, please select a valid order and set the status to 'Delivered'.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            foreach (var orderDetail in dataGridOrderDetails.Items.Cast<OrderDetails>())
            {
                orderDetail.Product.ProductQuantity -= orderDetail.OrderProductQuantity;

                if (orderDetail.Product.ProductQuantity < 0)
                {
                    MessageBox.Show($"The product '{orderDetail.Product.Name}' cannot be ordered in this quantity. Please reduce the quantity.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }
            }

            try
            {
                selectedOrder.Status = selectedStatus;
                await _orderRepository.SaveChangesAsync();
                await _orderDetailsRepository.SaveChangesAsync();
                await _productRepository.SaveChangesAsync();
                await LoadAllOrderDetails();
                await LoadAllOrdersHistory();
                await LoadAllProducts();
                await LoadProductsToCatalogAsync();
                ComboBoxStatus.IsEnabled = false;
                MessageBox.Show("Order successfully completed.", "Success", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"An error occurred while completing the order: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private async void ExportOrder_Click(object sender, RoutedEventArgs e)
        {
            SaveFileDialog saveFileDialog = new SaveFileDialog();
            saveFileDialog.Filter = "JSON files (*.json)|*.json|Excel files (*.xlsx)|*.xlsx|All files (*.*)|*.*";
            saveFileDialog.FilterIndex = 1;
            saveFileDialog.RestoreDirectory = true;

            if (saveFileDialog.ShowDialog() == true)
            {
                string filePath = saveFileDialog.FileName;

                try
                {
                    switch (saveFileDialog.FilterIndex)
                    {
                        case 1:
                            await ExportOrderToJsonAsync(filePath);
                            break;

                        case 2:
                            await ExportOrderToExcelAsync(filePath);
                            break;

                        default:
                            MessageBox.Show("Unsupported file format.");
                            break;
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show("Error exporting order: " + ex.Message);
                }
            }
        }

        private async Task ExportOrderToJsonAsync(string filePath)
        {
            Order selectedOrder = ComboBoxOrders.SelectedItem as Order;

            List<OrderDetails> orderDetails = await dbContext.OrderDetails
                .Include(od => od.Product)
                .Where(od => od.Order.Id == selectedOrder.Id)
                .ToListAsync();

            decimal totalOrderSum = orderDetails.Sum(od => (decimal)od.Price);

            var exportData = new List<object>();

            foreach (var detail in orderDetails)
            {
                var orderDetailData = new
                {
                    OrderDetailId = detail.Id,
                    OrderProductQuantity = detail.OrderProductQuantity,
                    Price = detail.Price,
                    ProductId = detail.Product.Id,
                    ProductName = detail.Product.Name,
                    ProductPrice = detail.Product.Price,
                    ProductImage = detail.Product.Image
                };

                exportData.Add(orderDetailData);
            }

            var exportObject = new
            {
                OrderId = selectedOrder.Id,
                OrderDate = selectedOrder.OrderDate,
                OrderStatus = selectedOrder.Status.Name,
                OrderDetails = exportData,
                TotalOrderSum = totalOrderSum
            };

            string json = JsonConvert.SerializeObject(exportObject, Formatting.Indented);

            await File.WriteAllTextAsync(filePath, json);

            MessageBox.Show("Order exported to JSON successfully.", "Success", MessageBoxButton.OK, MessageBoxImage.Information);
        }

        private async Task ExportOrderToExcelAsync(string filePath)
        {
            ExcelPackage.LicenseContext = LicenseContext.NonCommercial;

            Order selectedOrder = ComboBoxOrders.SelectedItem as Order;

            if (selectedOrder != null)
            {
                List<OrderDetails> orderDetails = await _orderDetailsRepository.GetQueryable()
                                                    .Where(od => od.Order.Id == selectedOrder.Id)
                                                    .ToListAsync();

                ExcelPackage excelPackage = new ExcelPackage();

                ExcelWorksheet worksheet = excelPackage.Workbook.Worksheets.Add("Order Details");

                worksheet.Cells[1, 1].Value = "Product ID";
                worksheet.Cells[1, 2].Value = "Product Name";
                worksheet.Cells[1, 3].Value = "Quantity";
                worksheet.Cells[1, 4].Value = "Price";
                worksheet.Cells[1, 5].Value = "Order ID";
                worksheet.Cells[1, 6].Value = "Total Order Sum";

                for (int i = 0; i < orderDetails.Count; i++)
                {
                    worksheet.Cells[i + 2, 1].Value = orderDetails[i].Product.Id;
                    worksheet.Cells[i + 2, 2].Value = orderDetails[i].Product.Name;
                    worksheet.Cells[i + 2, 3].Value = orderDetails[i].OrderProductQuantity;
                    worksheet.Cells[i + 2, 4].Value = orderDetails[i].Price;
                    worksheet.Cells[i + 2, 5].Value = selectedOrder.Id;
                }

                float totalOrderSum = orderDetails.Sum(od => od.Price);
                worksheet.Cells[orderDetails.Count + 2, 6].Value = totalOrderSum;

                await excelPackage.SaveAsAsync(new FileInfo(filePath));

                MessageBox.Show("Order exported to Excel successfully.", "Success", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            else
            {
                MessageBox.Show("Please select an order.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private async void RemoveDetailFromOrder_Click(object sender, RoutedEventArgs e)
        {
            // Отримання вибраного замовлення та деталей замовлення
            Order selectedOrder = ComboBoxOrders.SelectedItem as Order;
            OrderDetails selectedOrderDetail = dataGridOrderDetails.SelectedItem as OrderDetails;

            if (selectedOrder == null || selectedOrderDetail == null)
            {
                MessageBox.Show("Please select both an order and an order detail to remove.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            try
            {
                // Видалення деталі замовлення
                _orderDetailsRepository.Delete(selectedOrderDetail);
                await _orderDetailsRepository.SaveChangesAsync();

                // Оновлення списку деталей замовлення
                await LoadAllOrderDetails();

                MessageBox.Show("Order detail removed successfully.", "Success", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"An error occurred while removing order detail: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
    }
}

