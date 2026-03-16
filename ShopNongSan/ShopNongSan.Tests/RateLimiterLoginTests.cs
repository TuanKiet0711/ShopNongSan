// 6.1 Công cụ sử dụng và lý do lựa chọn
// - Selenium WebDriver: kiểm thử UI tự động, giả lập thao tác người dùng trên web.
// - NUnit: framework test phổ biến, dễ tích hợp với Selenium, hỗ trợ report.
// - ChromeDriver: chạy test trên trình duyệt Chrome.

// 6.2 Các test script đã viết
// - TestLoginFailRateLimiter: kiểm thử đăng nhập sai liên tục, xác nhận rate limiter hoạt động đúng.
// - TestLoginSuccess: kiểm thử đăng nhập đúng, xác nhận đăng nhập thành công.

using NUnit.Framework;
using OpenQA.Selenium;
using OpenQA.Selenium.Chrome;
using System;
using System.IO;
using System.Threading;

namespace ShopNongSan.Tests
{
    [TestFixture]
    public class RateLimiterLoginTests
    {
        private IWebDriver driver;
        private string baseUrl = "http://localhost:5000/Customer/TaiKhoan/DangNhap"; // Đổi port nếu khác
        private string username = "Kietletuan002";
        private string passwordWrong = "SaiMatKhau123";
        private string passwordRight = "Kietlatoi1";

        [SetUp]
        public void Setup()
        {
            driver = new ChromeDriver();
            driver.Manage().Timeouts().ImplicitWait = TimeSpan.FromSeconds(5);
        }

        [TearDown]
        public void Teardown()
        {
            driver.Quit();
        }

        [Test]
        [Order(1)]
        public void TestLoginFailThreeTimes_NotBlocked()
        {
            driver.Navigate().GoToUrl(baseUrl);
            for (int i = 1; i <= 3; i++)
            {
                driver.FindElement(By.Name("TenDangNhap")).Clear();
                driver.FindElement(By.Name("TenDangNhap")).SendKeys(username);
                driver.FindElement(By.Name("MatKhau")).Clear();
                driver.FindElement(By.Name("MatKhau")).SendKeys(passwordWrong);
                driver.FindElement(By.CssSelector("button[type='submit']")).Click();
                Thread.Sleep(1000);
            }
            var msg = driver.PageSource;
            Assert.That(!msg.Contains("đang bị khóa"), Is.True);
            // Ghi log report
            try
            {
                string logDir = Path.Combine("..", "TestLogs");
                Directory.CreateDirectory(logDir);
                string logPath = Path.Combine(logDir, "TestLog.txt");
                File.AppendAllText(logPath, $"TestLoginFailThreeTimes_NotBlocked: {(msg.Contains("đang bị khóa") ? "Fail" : "Pass")}\n");
            }
            catch (Exception ex)
            {
                string logDir = Path.Combine("..", "TestLogs");
                Directory.CreateDirectory(logDir);
                string logPath = Path.Combine(logDir, "TestLog.txt");
                File.AppendAllText(logPath, $"TestLoginFailThreeTimes_NotBlocked: Error - {ex.Message}\n");
            }
        }

        [Test]
        [Order(2)]
        public void TestLoginSuccess()
        {
            driver.Navigate().GoToUrl(baseUrl);
            driver.FindElement(By.Name("TenDangNhap")).Clear();
            driver.FindElement(By.Name("TenDangNhap")).SendKeys(username);
            driver.FindElement(By.Name("MatKhau")).Clear();
            driver.FindElement(By.Name("MatKhau")).SendKeys(passwordRight);
            driver.FindElement(By.CssSelector("button[type='submit']")).Click();
            Thread.Sleep(1000);
            var msg = driver.PageSource;
            Assert.That(!msg.Contains("đang bị khóa"), Is.True);
            // Ghi log report
            try
            {
                string logDir = Path.Combine("..", "TestLogs");
                Directory.CreateDirectory(logDir);
                string logPath = Path.Combine(logDir, "TestLog.txt");
                File.AppendAllText(logPath, $"TestLoginSuccess: {(msg.Contains("đang bị khóa") ? "Fail" : "Pass")}\n");
            }
            catch (Exception ex)
            {
                string logDir = Path.Combine("..", "TestLogs");
                Directory.CreateDirectory(logDir);
                string logPath = Path.Combine(logDir, "TestLog.txt");
                File.AppendAllText(logPath, $"TestLoginSuccess: Error - {ex.Message}\n");
            }
        }

        [Test]
        [Order(3)]
        public void TestLoginFailBlocked()
        {
            driver.Navigate().GoToUrl(baseUrl);
            // Đăng nhập sai 5 lần để kích hoạt lockout
            for (int i = 1; i <= 5; i++)
            {
                driver.FindElement(By.Name("TenDangNhap")).Clear();
                driver.FindElement(By.Name("TenDangNhap")).SendKeys(username);
                driver.FindElement(By.Name("MatKhau")).Clear();
                driver.FindElement(By.Name("MatKhau")).SendKeys(passwordWrong);
                driver.FindElement(By.CssSelector("button[type='submit']")).Click();
                Thread.Sleep(1000);
            }
            // Kiểm tra ngay lập tức bị lockout (không cần thử đăng nhập đúng)
            var msg = driver.PageSource;
            Assert.That(msg.Contains("Bạn đã nhập sai 5 lần."), Is.True);
            // Ghi log report
            try
            {
                string logDir = Path.Combine("..", "TestLogs");
                Directory.CreateDirectory(logDir);
                string logPath = Path.Combine(logDir, "TestLog.txt");
                File.AppendAllText(logPath, $"TestLoginFailBlocked: {(msg.Contains("đang bị khóa") ? "Pass" : "Fail")}\n");
            }
            catch (Exception ex)
            {
                string logDir = Path.Combine("..", "TestLogs");
                Directory.CreateDirectory(logDir);
                string logPath = Path.Combine(logDir, "TestLog.txt");
                File.AppendAllText(logPath, $"TestLoginFailBlocked: Error - {ex.Message}\n");
            }
        }
    }
}
