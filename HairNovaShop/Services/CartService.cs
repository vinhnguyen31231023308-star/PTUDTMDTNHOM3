using System.Text.Json;
using HairNovaShop.Models;

namespace HairNovaShop.Services;

public interface ICartService
{
    void AddToCart(ISession session, int productId, int quantity = 1, string? capacity = null);
    void UpdateQuantity(ISession session, int productId, int quantity, string? capacity = null);
    void RemoveFromCart(ISession session, int productId, string? capacity = null);
    List<CartItem> GetCartItems(ISession session);
    int GetCartItemCount(ISession session);
    void ClearCart(ISession session);
}

public class CartService : ICartService
{
    private readonly string _cartKey = "CartItems";

    public void AddToCart(ISession session, int productId, int quantity = 1, string? capacity = null)
    {
        var cartItems = GetCartItems(session);
        
        var existingItem = cartItems.FirstOrDefault(c => 
            c.ProductId == productId && 
            ((c.Capacity == null && capacity == null) || c.Capacity == capacity));

        if (existingItem != null)
        {
            existingItem.Quantity += quantity;
        }
        else
        {
            // Product info sẽ được load từ database trong controller
            cartItems.Add(new CartItem
            {
                ProductId = productId,
                Quantity = quantity,
                Capacity = capacity
            });
        }

        SaveCart(session, cartItems);
    }

    public void UpdateQuantity(ISession session, int productId, int quantity, string? capacity = null)
    {
        var cartItems = GetCartItems(session);
        var item = cartItems.FirstOrDefault(c => 
            c.ProductId == productId && 
            ((c.Capacity == null && capacity == null) || c.Capacity == capacity));

        if (item != null)
        {
            if (quantity <= 0)
            {
                cartItems.Remove(item);
            }
            else
            {
                item.Quantity = quantity;
            }
            SaveCart(session, cartItems);
        }
    }

    public void RemoveFromCart(ISession session, int productId, string? capacity = null)
    {
        var cartItems = GetCartItems(session);
        var item = cartItems.FirstOrDefault(c => 
            c.ProductId == productId && 
            ((c.Capacity == null && capacity == null) || c.Capacity == capacity));

        if (item != null)
        {
            cartItems.Remove(item);
            SaveCart(session, cartItems);
        }
    }

    public List<CartItem> GetCartItems(ISession session)
    {
        var cartJson = session.GetString(_cartKey);
        if (string.IsNullOrEmpty(cartJson))
        {
            return new List<CartItem>();
        }

        try
        {
            return JsonSerializer.Deserialize<List<CartItem>>(cartJson) ?? new List<CartItem>();
        }
        catch
        {
            return new List<CartItem>();
        }
    }

    public int GetCartItemCount(ISession session)
    {
        return GetCartItems(session).Sum(item => item.Quantity);
    }

    public void ClearCart(ISession session)
    {
        session.Remove(_cartKey);
    }

    private void SaveCart(ISession session, List<CartItem> cartItems)
    {
        var cartJson = JsonSerializer.Serialize(cartItems);
        session.SetString(_cartKey, cartJson);
    }
}
