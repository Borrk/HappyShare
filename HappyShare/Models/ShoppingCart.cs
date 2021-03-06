﻿using System;
using System.Linq;
using System.Collections.Generic;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Http;
using HappyShare.Data;

namespace HappyShare.Models
{
    public class ShoppingCart
    {
        public string ShoppingCartId { get; set; }
        public const string CartSessionKey = "cartId";

        public static ShoppingCart GetCart(HttpContext context)
        {
            var cart = new ShoppingCart();
            cart.ShoppingCartId = cart.GetCartId(context);
            return cart;
        }

        public void AddToCart(SharedItem item, ApplicationDbContext db)
        {
            var cartItem = db.CartItems.SingleOrDefault(c => c.CartID == ShoppingCartId && c.SharedItem.ID == item.ID);
            if (cartItem == null)
            {
                cartItem = new CartItem
                {
                    SharedItem = item,
                    CartID = ShoppingCartId,
                    ProductID = item.ID
                };
                db.CartItems.Add(cartItem);
            }
            else
            {
                // do nothing
                //cartItem.Count++;
            }
            db.SaveChanges();
        }

        public int RemoveFromCart(int id, ApplicationDbContext db)
        {
            var cartItem = db.CartItems.SingleOrDefault(cart => cart.CartID == ShoppingCartId && cart.SharedItem.ID == id);
            int itemCount = 0;
            if (cartItem != null)
            {
                db.CartItems.Remove(cartItem);
                db.SaveChanges();
            }
            return itemCount;
        }

        public void EmptyCart(ApplicationDbContext db)
        {
            var cartItems = db.CartItems.Where(cart => cart.CartID == ShoppingCartId);
            foreach (var cartItem in cartItems)
            {
                db.CartItems.Remove(cartItem);
            }
            db.SaveChanges();
        }
        public List<CartItem> GetCartItems(ApplicationDbContext db)
        {
            List<CartItem> cartItems = db.CartItems.Include(i => i.SharedItem).ThenInclude(p=>p.Category).Where(cartItem => cartItem.CartID == ShoppingCartId).ToList();

            return cartItems;

        }

        public void ResetShoppingcartId(string newId, HttpContext context)
        {
            if (ShoppingCartId == newId)
            {
                return;
            }

            // set cart id to new one
            ShoppingCartId = newId;
            context.Session.SetString(CartSessionKey, ShoppingCartId);
        }
        public void ResetShoppingcartId(string newId, HttpContext context, ApplicationDbContext db)
        {
            if (ShoppingCartId == newId)
            {
                return;
            }

            // firstly, we need check if all the products in our cart are avaliable.
            var cit = db.CartItems.Where(i => i.ProductID >= 0).AsNoTracking().ToList();
            foreach (var i in cit)
            {
                var product = db.SharedItems.SingleOrDefault(p => p.ID == i.ProductID);
                if (product == null)
                {
                    db.CartItems.Remove(i);
                }
            }
            db.SaveChanges();

            var cartItems = db.CartItems.Where(cart => cart.CartID == ShoppingCartId);
            foreach (var cartItem in cartItems)
            {
                // check if product is avaliable
                var product = db.SharedItems.SingleOrDefault(p => p.ID == cartItem.ProductID);
                if (product == null)
                {
                    db.CartItems.Remove(cartItem);
                    continue;
                }

                cartItem.CartID = newId;
                var ci = db.CartItems.SingleOrDefault(c => c.CartID == newId && c.ProductID == cartItem.ProductID);
                if (ci == null)
                {
                    db.Update(cartItem);
                }
                else  // merge cart items
                {
                    db.Update(ci);

                    db.CartItems.Remove(cartItem);
                }
            }

            db.SaveChanges();

            // set cart id to new one
            ShoppingCartId = newId;
            context.Session.SetString(CartSessionKey, ShoppingCartId);

            //var cartItems = db.CartItems.Include( i => i.Product).Where(cart => cart.CartID == ShoppingCartId);
            //foreach (var cartItem in cartItems)
            //{
            //    cartItem.CartID = newId;

            //    var ci = db.CartItems.SingleOrDefault(c => c.CartID == newId && c.Product.ProductID == cartItem.Product.ProductID);
            //    if (ci == null)
            //    {
            //        db.Update(cartItem);
            //    }
            //    else  // merge cart items
            //    {
            //        ci.Count += cartItem.Count;
            //        db.Update(ci);

            //        db.CartItems.Remove(cartItem);
            //    }                
            //}

            //db.SaveChanges();

            //// set cart id to new one
            //ShoppingCartId = newId;
            //context.Session.SetString(CartSessionKey, ShoppingCartId);
        }

        public int GetCount(ApplicationDbContext db)
        {
            int? count =
                (from cartItem in db.CartItems where cartItem.CartID == ShoppingCartId select cartItem ).Count();
            return count ?? 0;
        }

        public decimal GetTotal(ApplicationDbContext db)
        {
            decimal? total = (from cartItems in db.CartItems
                              where cartItems.CartID == ShoppingCartId select cartItems).Count();
            return total ?? decimal.Zero;
        }

        public string GetCartId(HttpContext context)
        {
            if (context.Session.GetString(CartSessionKey) == null)
            {
                Guid tempCartId = Guid.NewGuid();
                context.Session.SetString(CartSessionKey, tempCartId.ToString());
            }
            return context.Session.GetString(CartSessionKey).ToString();
        }
    }

}
