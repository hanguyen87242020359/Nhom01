namespace ShopBanHoaLyly.Services
{
    /// <summary>
    /// Cung cấp hàm tính phí vận chuyển thống nhất cho toàn bộ hệ thống.
    /// </summary>
    public static class ShippingCalculator
    {
        public const decimal FreeShippingThreshold = 500_000M;
        public const decimal StandardShippingFee  = 30_000M;

        /// <summary>
        /// Trả về phí vận chuyển dựa trên tạm tính.
        /// <para>Miễn phí nếu &gt;= 500.000₫, ngược lại 30.000₫.</para>
        /// </summary>
        public static decimal Calculate(decimal subtotal)
        {
            return subtotal >= FreeShippingThreshold ? 0 : StandardShippingFee;
        }
    }
} 