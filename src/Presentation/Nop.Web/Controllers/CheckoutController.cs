﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using DocumentFormat.OpenXml.Office.CustomUI;
using DocumentFormat.OpenXml.Presentation;
using Hangfire.Annotations;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using Nop.Core;
using Nop.Core.Domain.Common;
using Nop.Core.Domain.Customers;
using Nop.Core.Domain.Directory;
using Nop.Core.Domain.Orders;
using Nop.Core.Domain.Payments;
using Nop.Core.Domain.Shipping;
using Nop.Core.Http.Extensions;
using Nop.Services.Catalog;
using Nop.Services.Common;
using Nop.Services.Customers;
using Nop.Services.Directory;
using Nop.Services.Localization;
using Nop.Services.Logging;
using Nop.Services.Orders;
using Nop.Services.Payments;
using Nop.Services.Shipping;
using Nop.Unleash.Common;
using Nop.Unleash.Models;
using Nop.Unleash.Service;
using Nop.Web.Extensions;
using Nop.Web.Factories;
using Nop.Web.Framework.Controllers;
using Nop.Web.Framework.Infrastructure;
using Nop.Web.Models.Checkout;
using Nop.Web.Models.Common;


namespace Nop.Web.Controllers
{
    [AutoValidateAntiforgeryToken]
    public partial class CheckoutController : BasePublicController
    {
        #region Fields

        private readonly AddressSettings _addressSettings;
        private readonly CustomerSettings _customerSettings;
        private readonly IAddressAttributeParser _addressAttributeParser;
        private readonly IAddressModelFactory _addressModelFactory;
        private readonly IAddressService _addressService;
        private readonly ICheckoutModelFactory _checkoutModelFactory;
        private readonly ICountryService _countryService;
        private readonly ICustomerService _customerService;
        private readonly IGenericAttributeService _genericAttributeService;
        private readonly ILocalizationService _localizationService;
        private readonly ILogger _logger;
        private readonly IOrderProcessingService _orderProcessingService;
        private readonly IOrderService _orderService;
        private readonly IPaymentPluginManager _paymentPluginManager;
        private readonly IPaymentService _paymentService;
        private readonly IProductService _productService;
        private readonly IShippingService _shippingService;
        private readonly IShoppingCartService _shoppingCartService;
        private readonly IStoreContext _storeContext;
        private readonly IWebHelper _webHelper;
        private readonly IWorkContext _workContext;
        private readonly OrderSettings _orderSettings;
        private readonly PaymentSettings _paymentSettings;
        private readonly RewardPointsSettings _rewardPointsSettings;
        private readonly ShippingSettings _shippingSettings;
        private readonly IProductUnleashService _productUnleashService;
        private readonly IStateProvinceService _stateProvinceService;

        #endregion

        #region Ctor

        public CheckoutController(AddressSettings addressSettings,
            CustomerSettings customerSettings,
            IAddressAttributeParser addressAttributeParser,
            IAddressModelFactory addressModelFactory,
            IAddressService addressService,
            ICheckoutModelFactory checkoutModelFactory,
            ICountryService countryService,
            ICustomerService customerService,
            IGenericAttributeService genericAttributeService,
            ILocalizationService localizationService,
            ILogger logger,
            IOrderProcessingService orderProcessingService,
            IOrderService orderService,
            IPaymentPluginManager paymentPluginManager,
            IPaymentService paymentService,
            IProductService productService,
            IShippingService shippingService,
            IShoppingCartService shoppingCartService,
            IStoreContext storeContext,
            IWebHelper webHelper,
            IWorkContext workContext,
            OrderSettings orderSettings,
            PaymentSettings paymentSettings,
            RewardPointsSettings rewardPointsSettings,
            ShippingSettings shippingSettings,
            IStateProvinceService stateProvinceService,
            IProductUnleashService productUnleashService)
        {
            _addressSettings = addressSettings;
            _customerSettings = customerSettings;
            _addressAttributeParser = addressAttributeParser;
            _addressModelFactory = addressModelFactory;
            _addressService = addressService;
            _checkoutModelFactory = checkoutModelFactory;
            _countryService = countryService;
            _customerService = customerService;
            _genericAttributeService = genericAttributeService;
            _localizationService = localizationService;
            _logger = logger;
            _orderProcessingService = orderProcessingService;
            _orderService = orderService;
            _paymentPluginManager = paymentPluginManager;
            _paymentService = paymentService;
            _productService = productService;
            _shippingService = shippingService;
            _shoppingCartService = shoppingCartService;
            _storeContext = storeContext;
            _webHelper = webHelper;
            _workContext = workContext;
            _orderSettings = orderSettings;
            _paymentSettings = paymentSettings;
            _rewardPointsSettings = rewardPointsSettings;
            _shippingSettings = shippingSettings;
            _productUnleashService = productUnleashService;
            _stateProvinceService = stateProvinceService;
    }

        #endregion

        #region Utilities

        protected virtual async Task<bool> IsMinimumOrderPlacementIntervalValidAsync(Nop.Core.Domain.Customers.Customer customer)
        {
            //prevent 2 orders being placed within an X seconds time frame
            if (_orderSettings.MinimumOrderPlacementInterval == 0)
                return true;

            var store = await _storeContext.GetCurrentStoreAsync();

            var lastOrder = (await _orderService.SearchOrdersAsync(storeId: store.Id,
                customerId: customer.Id, pageSize: 1))
                .FirstOrDefault();
            if (lastOrder == null)
                return true;

            var interval = DateTime.UtcNow - lastOrder.CreatedOnUtc;
            return interval.TotalSeconds > _orderSettings.MinimumOrderPlacementInterval;
        }

        /// <summary>
        /// Parses the value indicating whether the "pickup in store" is allowed
        /// </summary>
        /// <param name="form">The form</param>
        /// <returns>The value indicating whether the "pickup in store" is allowed</returns>
        protected virtual bool ParsePickupInStore(IFormCollection form)
        {
            var pickupInStore = false;

            var pickupInStoreParameter = form["PickupInStore"].FirstOrDefault();
            if (!string.IsNullOrWhiteSpace(pickupInStoreParameter))
                _ = bool.TryParse(pickupInStoreParameter, out pickupInStore);

            return pickupInStore;
        }

        /// <summary>
        /// Parses the pickup option
        /// </summary>
        /// <param name="form">The form</param>
        /// <returns>
        /// The task result contains the pickup option
        /// </returns>
        protected virtual async Task<PickupPoint> ParsePickupOptionAsync(IFormCollection form)
        {
            var pickupPoint = form["pickup-points-id"].ToString().Split(new[] { "___" }, StringSplitOptions.None);

            var customer = await _workContext.GetCurrentCustomerAsync();
            var store = await _storeContext.GetCurrentStoreAsync();
            var selectedPoint = (await _shippingService.GetPickupPointsAsync(customer.BillingAddressId ?? 0,
                customer, pickupPoint[1], store.Id)).PickupPoints.FirstOrDefault(x => x.Id.Equals(pickupPoint[0]));

            if (selectedPoint == null)
                throw new Exception("Pickup point is not allowed");

            return selectedPoint;
        }

        /// <summary>
        /// Saves the pickup option
        /// </summary>
        /// <param name="pickupPoint">The pickup option</param>
        protected virtual async Task SavePickupOptionAsync(PickupPoint pickupPoint)
        {
            var name = !string.IsNullOrEmpty(pickupPoint.Name) ?
                string.Format(await _localizationService.GetResourceAsync("Checkout.PickupPoints.Name"), pickupPoint.Name) :
                await _localizationService.GetResourceAsync("Checkout.PickupPoints.NullName");
            var pickUpInStoreShippingOption = new ShippingOption
            {
                Name = name,
                Rate = pickupPoint.PickupFee,
                Description = pickupPoint.Description,
                ShippingRateComputationMethodSystemName = pickupPoint.ProviderSystemName,
                IsPickupInStore = true
            };

            var customer = await _workContext.GetCurrentCustomerAsync();
            var store = await _storeContext.GetCurrentStoreAsync();
            await _genericAttributeService.SaveAttributeAsync(customer, NopCustomerDefaults.SelectedShippingOptionAttribute, pickUpInStoreShippingOption, store.Id);
            await _genericAttributeService.SaveAttributeAsync(customer, NopCustomerDefaults.SelectedPickupPointAttribute, pickupPoint, store.Id);
        }

        #endregion

        #region Methods (common)

        public virtual async Task<IActionResult> Index()
        {
            //validation
            if (_orderSettings.CheckoutDisabled)
                return RedirectToRoute("ShoppingCart");

            var customer = await _workContext.GetCurrentCustomerAsync();
            var store = await _storeContext.GetCurrentStoreAsync();
            var cart = await _shoppingCartService.GetShoppingCartAsync(customer, ShoppingCartType.ShoppingCart, store.Id);

            if (!cart.Any())
                return RedirectToRoute("ShoppingCart");

            var cartProductIds = cart.Select(ci => ci.ProductId).ToArray();
            var downloadableProductsRequireRegistration =
                _customerSettings.RequireRegistrationForDownloadableProducts && await _productService.HasAnyDownloadableProductAsync(cartProductIds);

            if (await _customerService.IsGuestAsync(customer) && (!_orderSettings.AnonymousCheckoutAllowed || downloadableProductsRequireRegistration))
                return Challenge();

            //if we have only "button" payment methods available (displayed on the shopping cart page, not during checkout),
            //then we should allow standard checkout
            //all payment methods (do not filter by country here as it could be not specified yet)
            var paymentMethods = await (await _paymentPluginManager
                .LoadActivePluginsAsync(customer, store.Id))
                .WhereAwait(async pm => !await pm.HidePaymentMethodAsync(cart)).ToListAsync();
            //payment methods displayed during checkout (not with "Button" type)
            var nonButtonPaymentMethods = paymentMethods
                .Where(pm => pm.PaymentMethodType != PaymentMethodType.Button)
                .ToList();
            //"button" payment methods(*displayed on the shopping cart page)
            var buttonPaymentMethods = paymentMethods
                .Where(pm => pm.PaymentMethodType == PaymentMethodType.Button)
                .ToList();
            if (!nonButtonPaymentMethods.Any() && buttonPaymentMethods.Any())
                return RedirectToRoute("ShoppingCart");

            //reset checkout data
            await _customerService.ResetCheckoutDataAsync(customer, store.Id);

            //validation (cart)
            var checkoutAttributesXml = await _genericAttributeService.GetAttributeAsync<string>(customer,
                NopCustomerDefaults.CheckoutAttributes, store.Id);
            var scWarnings = await _shoppingCartService.GetShoppingCartWarningsAsync(cart, checkoutAttributesXml, true);
            if (scWarnings.Any())
                return RedirectToRoute("ShoppingCart");
            //validation (each shopping cart item)
            foreach (var sci in cart)
            {
                var product = await _productService.GetProductByIdAsync(sci.ProductId);

                var sciWarnings = await _shoppingCartService.GetShoppingCartItemWarningsAsync(customer,
                    sci.ShoppingCartType,
                    product,
                    sci.StoreId,
                    sci.AttributesXml,
                    sci.CustomerEnteredPrice,
                    sci.RentalStartDateUtc,
                    sci.RentalEndDateUtc,
                    sci.Quantity,
                    false,
                    sci.Id);
                if (sciWarnings.Any())
                    return RedirectToRoute("ShoppingCart");
            }

            if (_orderSettings.OnePageCheckoutEnabled)
                return RedirectToRoute("CheckoutOnePage");

            return RedirectToRoute("CheckoutBillingAddress");
        }

        public virtual async Task<IActionResult> Completed(int? orderId)
        {
            //validation
            var customer = await _workContext.GetCurrentCustomerAsync();
            if (await _customerService.IsGuestAsync(customer) && !_orderSettings.AnonymousCheckoutAllowed)
                return Challenge();

            Order order = null;
            if (orderId.HasValue)
            {
                //load order by identifier (if provided)
                order = await _orderService.GetOrderByIdAsync(orderId.Value);
            }
            if (order == null)
            {
                var store = await _storeContext.GetCurrentStoreAsync();
                order = (await _orderService.SearchOrdersAsync(storeId: store.Id,
                customerId: customer.Id, pageSize: 1))
                    .FirstOrDefault();
            }
            if (order == null || order.Deleted || customer.Id != order.CustomerId)
            {
                return RedirectToRoute("Homepage");
            }

            //disable "order completed" page?
            if (_orderSettings.DisableOrderCompletedPage)
            {
                return RedirectToRoute("OrderDetails", new { orderId = order.Id });
            }

            //model
            var model = await _checkoutModelFactory.PrepareCheckoutCompletedModelAsync(order);
            return View(model);
        }

        /// <summary>
        /// Get specified Address by addresId
        /// </summary>
        /// <param name="addressId"></param>
        public virtual async Task<IActionResult> GetAddressById(int addressId)
        {
            var customer = await _workContext.GetCurrentCustomerAsync();
            var address = await _customerService.GetCustomerAddressAsync(customer.Id, addressId);
            if (address == null)
                throw new ArgumentNullException(nameof(address));

            var addressModel = new AddressModel();

            await _addressModelFactory.PrepareAddressModelAsync(addressModel,
                address: address,
                excludeProperties: false,
                addressSettings: _addressSettings,
                prePopulateWithCustomerFields: true,
                customer: customer);

            var json = JsonConvert.SerializeObject(addressModel, Formatting.Indented,
                new JsonSerializerSettings
                {
                    ReferenceLoopHandling = ReferenceLoopHandling.Ignore
                });

            return Content(json, "application/json");
        }

        /// <summary>
        /// Save edited address
        /// </summary>
        /// <param name="model"></param>
        /// <param name="opc"></param>
        /// <returns></returns>
        public virtual async Task<IActionResult> SaveEditAddress(CheckoutBillingAddressModel model, IFormCollection form, bool opc = false)
        {
            try
            {
                var customer = await _workContext.GetCurrentCustomerAsync();
                var store = await _storeContext.GetCurrentStoreAsync();
                var cart = await _shoppingCartService.GetShoppingCartAsync(customer, ShoppingCartType.ShoppingCart, store.Id);
                if (!cart.Any())
                    throw new Exception("Your cart is empty");

                //find address (ensure that it belongs to the current customer)
                var address = await _customerService.GetCustomerAddressAsync(customer.Id, model.BillingNewAddress.Id);
                if (address == null)
                    throw new Exception("Address can't be loaded");

                //custom address attributes
                var customAttributes = await _addressAttributeParser.ParseCustomAddressAttributesAsync(form);
                var customAttributeWarnings = await _addressAttributeParser.GetAttributeWarningsAsync(customAttributes);

                if (customAttributeWarnings.Any())
                {
                    return Json(new { error = 1, message = customAttributeWarnings });
                }

                address = model.BillingNewAddress.ToEntity(address);
                address.CustomAttributes = customAttributes;

                await _addressService.UpdateAddressAsync(address);

                customer.BillingAddressId = address.Id;
                await _customerService.UpdateCustomerAsync(customer);

                if (!opc)
                {
                    return Json(new
                    {
                        redirect = Url.RouteUrl("CheckoutBillingAddress")
                    });
                }

                var billingAddressModel = await _checkoutModelFactory.PrepareBillingAddressModelAsync(cart, address.CountryId);
                return Json(new
                {
                    selected_id = model.BillingNewAddress.Id,
                    update_section = new UpdateSectionJsonModel
                    {
                        name = "billing",
                        html = await RenderPartialViewToStringAsync("OpcBillingAddress", billingAddressModel)
                    }
                });
            }
            catch (Exception exc)
            {
                await _logger.WarningAsync(exc.Message, exc, await _workContext.GetCurrentCustomerAsync());
                return Json(new { error = 1, message = exc.Message });
            }
        }

        /// <summary>
        /// Delete edited address
        /// </summary>
        /// <param name="addressId"></param>
        /// <param name="opc"></param>
        public virtual async Task<IActionResult> DeleteEditAddress(int addressId, bool opc = false)
        {
            var customer = await _workContext.GetCurrentCustomerAsync();
            var store = await _storeContext.GetCurrentStoreAsync();
            var cart = await _shoppingCartService.GetShoppingCartAsync(customer, ShoppingCartType.ShoppingCart, store.Id);
            if (!cart.Any())
                throw new Exception("Your cart is empty");

            var address = await _customerService.GetCustomerAddressAsync(customer.Id, addressId);
            if (address != null)
            {
                await _customerService.RemoveCustomerAddressAsync(customer, address);
                await _customerService.UpdateCustomerAsync(customer);
                await _addressService.DeleteAddressAsync(address);
            }

            if (!opc)
            {
                return Json(new
                {
                    redirect = Url.RouteUrl("CheckoutBillingAddress")
                });
            }

            var billingAddressModel = await _checkoutModelFactory.PrepareBillingAddressModelAsync(cart);
            return Json(new
            {
                update_section = new UpdateSectionJsonModel
                {
                    name = "billing",
                    html = await RenderPartialViewToStringAsync("OpcBillingAddress", billingAddressModel)
                }
            });
        }

        #endregion

        #region Methods (multistep checkout)

        public virtual async Task<IActionResult> BillingAddress(IFormCollection form)
        {
            //validation
            if (_orderSettings.CheckoutDisabled)
                return RedirectToRoute("ShoppingCart");

            var customer = await _workContext.GetCurrentCustomerAsync();
            var store = await _storeContext.GetCurrentStoreAsync();
            var cart = await _shoppingCartService.GetShoppingCartAsync(customer, ShoppingCartType.ShoppingCart, store.Id);

            if (!cart.Any())
                return RedirectToRoute("ShoppingCart");

            if (_orderSettings.OnePageCheckoutEnabled)
                return RedirectToRoute("CheckoutOnePage");

            if (await _customerService.IsGuestAsync(customer) && !_orderSettings.AnonymousCheckoutAllowed)
                return Challenge();

            //model
            var model = await _checkoutModelFactory.PrepareBillingAddressModelAsync(cart, prePopulateNewAddressWithCustomerFields: true);

            //check whether "billing address" step is enabled
            if (_orderSettings.DisableBillingAddressCheckoutStep && model.ExistingAddresses.Any())
            {
                if (model.ExistingAddresses.Any())
                {
                    //choose the first one
                    return await SelectBillingAddress(model.ExistingAddresses.First().Id);
                }

                TryValidateModel(model);
                TryValidateModel(model.BillingNewAddress);
                return await NewBillingAddress(model, form);
            }

            return View(model);
        }

        public virtual async Task<IActionResult> SelectBillingAddress(int addressId, bool shipToSameAddress = false)
        {
            //validation
            if (_orderSettings.CheckoutDisabled)
                return RedirectToRoute("ShoppingCart");

            var customer = await _workContext.GetCurrentCustomerAsync();
            var address = await _customerService.GetCustomerAddressAsync(customer.Id, addressId);

            if (address == null)
                return RedirectToRoute("CheckoutBillingAddress");

            customer.BillingAddressId = address.Id;
            await _customerService.UpdateCustomerAsync(customer);

            var store = await _storeContext.GetCurrentStoreAsync();
            var cart = await _shoppingCartService.GetShoppingCartAsync(customer, ShoppingCartType.ShoppingCart, store.Id);

            //ship to the same address?
            //by default Shipping is available if the country is not specified
            var shippingAllowed = !_addressSettings.CountryEnabled || ((await _countryService.GetCountryByAddressAsync(address))?.AllowsShipping ?? false);
            if (_shippingSettings.ShipToSameAddress && shipToSameAddress && await _shoppingCartService.ShoppingCartRequiresShippingAsync(cart) && shippingAllowed)
            {
                customer.ShippingAddressId = customer.BillingAddressId;
                await _customerService.UpdateCustomerAsync(customer);
                //reset selected shipping method (in case if "pick up in store" was selected)
                await _genericAttributeService.SaveAttributeAsync<ShippingOption>(customer, NopCustomerDefaults.SelectedShippingOptionAttribute, null, store.Id);
                await _genericAttributeService.SaveAttributeAsync<PickupPoint>(customer, NopCustomerDefaults.SelectedPickupPointAttribute, null, store.Id);
                //limitation - "Ship to the same address" doesn't properly work in "pick up in store only" case (when no shipping plugins are available) 
                return RedirectToRoute("CheckoutShippingMethod");
            }

            return RedirectToRoute("CheckoutShippingAddress");
        }

        [HttpPost, ActionName("BillingAddress")]
        [FormValueRequired("nextstep")]
        public virtual async Task<IActionResult> NewBillingAddress(CheckoutBillingAddressModel model, IFormCollection form)
        {
            //validation
            if (_orderSettings.CheckoutDisabled)
                return RedirectToRoute("ShoppingCart");

            var customer = await _workContext.GetCurrentCustomerAsync();
            var store = await _storeContext.GetCurrentStoreAsync();
            var cart = await _shoppingCartService.GetShoppingCartAsync(customer, ShoppingCartType.ShoppingCart, store.Id);

            if (!cart.Any())
                return RedirectToRoute("ShoppingCart");

            if (_orderSettings.OnePageCheckoutEnabled)
                return RedirectToRoute("CheckoutOnePage");

            if (await _customerService.IsGuestAsync(customer) && !_orderSettings.AnonymousCheckoutAllowed)
                return Challenge();

            //custom address attributes
            var customAttributes = await _addressAttributeParser.ParseCustomAddressAttributesAsync(form);
            var customAttributeWarnings = await _addressAttributeParser.GetAttributeWarningsAsync(customAttributes);
            foreach (var error in customAttributeWarnings)
            {
                ModelState.AddModelError("", error);
            }

            var newAddress = model.BillingNewAddress;

            if (ModelState.IsValid)
            {
                //try to find an address with the same values (don't duplicate records)
                var address = _addressService.FindAddress((await _customerService.GetAddressesByCustomerIdAsync(customer.Id)).ToList(),
                    newAddress.FirstName, newAddress.LastName, newAddress.PhoneNumber,
                    newAddress.Email, newAddress.FaxNumber, newAddress.Company,
                    newAddress.Address1, newAddress.Address2, newAddress.City,
                    newAddress.County, newAddress.StateProvinceId, newAddress.ZipPostalCode,
                    newAddress.CountryId, customAttributes);

                if (address == null)
                {
                    //address is not found. let's create a new one
                    address = newAddress.ToEntity();
                    address.CustomAttributes = customAttributes;
                    address.CreatedOnUtc = DateTime.UtcNow;

                    //some validation
                    if (address.CountryId == 0)
                        address.CountryId = null;
                    if (address.StateProvinceId == 0)
                        address.StateProvinceId = null;

                    await _addressService.InsertAddressAsync(address);

                    await _customerService.InsertCustomerAddressAsync(customer, address);
                }

                customer.BillingAddressId = address.Id;

                await _customerService.UpdateCustomerAsync(customer);

                //ship to the same address?
                if (_shippingSettings.ShipToSameAddress && model.ShipToSameAddress && await _shoppingCartService.ShoppingCartRequiresShippingAsync(cart))
                {
                    customer.ShippingAddressId = customer.BillingAddressId;
                    await _customerService.UpdateCustomerAsync(customer);

                    //reset selected shipping method (in case if "pick up in store" was selected)
                    await _genericAttributeService.SaveAttributeAsync<ShippingOption>(customer, NopCustomerDefaults.SelectedShippingOptionAttribute, null, store.Id);
                    await _genericAttributeService.SaveAttributeAsync<PickupPoint>(customer, NopCustomerDefaults.SelectedPickupPointAttribute, null, store.Id);

                    //limitation - "Ship to the same address" doesn't properly work in "pick up in store only" case (when no shipping plugins are available) 
                    return RedirectToRoute("CheckoutShippingMethod");
                }

                return RedirectToRoute("CheckoutShippingAddress");
            }

            //If we got this far, something failed, redisplay form
            model = await _checkoutModelFactory.PrepareBillingAddressModelAsync(cart,
                selectedCountryId: newAddress.CountryId,
                overrideAttributesXml: customAttributes);
            return View(model);
        }

        public virtual async Task<IActionResult> ShippingAddress()
        {
            //validation
            if (_orderSettings.CheckoutDisabled)
                return RedirectToRoute("ShoppingCart");

            var customer = await _workContext.GetCurrentCustomerAsync();
            var store = await _storeContext.GetCurrentStoreAsync();
            var cart = await _shoppingCartService.GetShoppingCartAsync(customer, ShoppingCartType.ShoppingCart, store.Id);

            if (!cart.Any())
                return RedirectToRoute("ShoppingCart");

            if (_orderSettings.OnePageCheckoutEnabled)
                return RedirectToRoute("CheckoutOnePage");

            if (await _customerService.IsGuestAsync(customer) && !_orderSettings.AnonymousCheckoutAllowed)
                return Challenge();

            if (!await _shoppingCartService.ShoppingCartRequiresShippingAsync(cart))
                return RedirectToRoute("CheckoutShippingMethod");

            //model
            var model = await _checkoutModelFactory.PrepareShippingAddressModelAsync(cart, prePopulateNewAddressWithCustomerFields: true);
            return View(model);
        }

        public virtual async Task<IActionResult> SelectShippingAddress(int addressId)
        {
            //validation
            if (_orderSettings.CheckoutDisabled)
                return RedirectToRoute("ShoppingCart");

            var customer = await _workContext.GetCurrentCustomerAsync();
            var address = await _customerService.GetCustomerAddressAsync(customer.Id, addressId);

            if (address == null)
                return RedirectToRoute("CheckoutShippingAddress");

            customer.ShippingAddressId = address.Id;
            await _customerService.UpdateCustomerAsync(customer);

            if (_shippingSettings.AllowPickupInStore)
            {
                var store = await _storeContext.GetCurrentStoreAsync();
                //set value indicating that "pick up in store" option has not been chosen
                await _genericAttributeService.SaveAttributeAsync<PickupPoint>(customer, NopCustomerDefaults.SelectedPickupPointAttribute, null, store.Id);
            }

            return RedirectToRoute("CheckoutShippingMethod");
        }

        [HttpPost, ActionName("ShippingAddress")]
        [FormValueRequired("nextstep")]
        public virtual async Task<IActionResult> NewShippingAddress(CheckoutShippingAddressModel model, IFormCollection form)
        {
            //validation
            if (_orderSettings.CheckoutDisabled)
                return RedirectToRoute("ShoppingCart");

            var customer = await _workContext.GetCurrentCustomerAsync();
            var store = await _storeContext.GetCurrentStoreAsync();
            var cart = await _shoppingCartService.GetShoppingCartAsync(customer, ShoppingCartType.ShoppingCart, store.Id);

            if (!cart.Any())
                return RedirectToRoute("ShoppingCart");

            if (_orderSettings.OnePageCheckoutEnabled)
                return RedirectToRoute("CheckoutOnePage");

            if (await _customerService.IsGuestAsync(customer) && !_orderSettings.AnonymousCheckoutAllowed)
                return Challenge();

            if (!await _shoppingCartService.ShoppingCartRequiresShippingAsync(cart))
                return RedirectToRoute("CheckoutShippingMethod");

            //pickup point
            if (_shippingSettings.AllowPickupInStore && !_orderSettings.DisplayPickupInStoreOnShippingMethodPage)
            {
                var pickupInStore = ParsePickupInStore(form);
                if (pickupInStore)
                {
                    var pickupOption = await ParsePickupOptionAsync(form);
                    await SavePickupOptionAsync(pickupOption);

                    return RedirectToRoute("CheckoutPaymentMethod");
                }

                //set value indicating that "pick up in store" option has not been chosen
                await _genericAttributeService.SaveAttributeAsync<PickupPoint>(customer, NopCustomerDefaults.SelectedPickupPointAttribute, null, store.Id);
            }

            //custom address attributes
            var customAttributes = await _addressAttributeParser.ParseCustomAddressAttributesAsync(form);
            var customAttributeWarnings = await _addressAttributeParser.GetAttributeWarningsAsync(customAttributes);
            foreach (var error in customAttributeWarnings)
            {
                ModelState.AddModelError("", error);
            }

            var newAddress = model.ShippingNewAddress;

            if (ModelState.IsValid)
            {
                //try to find an address with the same values (don't duplicate records)
                var address = _addressService.FindAddress((await _customerService.GetAddressesByCustomerIdAsync(customer.Id)).ToList(),
                    newAddress.FirstName, newAddress.LastName, newAddress.PhoneNumber,
                    newAddress.Email, newAddress.FaxNumber, newAddress.Company,
                    newAddress.Address1, newAddress.Address2, newAddress.City,
                    newAddress.County, newAddress.StateProvinceId, newAddress.ZipPostalCode,
                    newAddress.CountryId, customAttributes);

                if (address == null)
                {
                    address = newAddress.ToEntity();
                    address.CustomAttributes = customAttributes;
                    address.CreatedOnUtc = DateTime.UtcNow;
                    //some validation
                    if (address.CountryId == 0)
                        address.CountryId = null;
                    if (address.StateProvinceId == 0)
                        address.StateProvinceId = null;

                    await _addressService.InsertAddressAsync(address);

                    await _customerService.InsertCustomerAddressAsync(customer, address);

                }

                customer.ShippingAddressId = address.Id;
                await _customerService.UpdateCustomerAsync(customer);

                return RedirectToRoute("CheckoutShippingMethod");
            }

            //If we got this far, something failed, redisplay form
            model = await _checkoutModelFactory.PrepareShippingAddressModelAsync(cart,
                selectedCountryId: newAddress.CountryId,
                overrideAttributesXml: customAttributes);
            return View(model);
        }

        public virtual async Task<IActionResult> ShippingMethod()
        {
            //validation
            if (_orderSettings.CheckoutDisabled)
                return RedirectToRoute("ShoppingCart");

            var customer = await _workContext.GetCurrentCustomerAsync();
            var store = await _storeContext.GetCurrentStoreAsync();
            var cart = await _shoppingCartService.GetShoppingCartAsync(customer, ShoppingCartType.ShoppingCart, store.Id);

            if (!cart.Any())
                return RedirectToRoute("ShoppingCart");

            if (_orderSettings.OnePageCheckoutEnabled)
                return RedirectToRoute("CheckoutOnePage");

            if (await _customerService.IsGuestAsync(customer) && !_orderSettings.AnonymousCheckoutAllowed)
                return Challenge();

            if (!await _shoppingCartService.ShoppingCartRequiresShippingAsync(cart))
            {
                await _genericAttributeService.SaveAttributeAsync<ShippingOption>(customer, NopCustomerDefaults.SelectedShippingOptionAttribute, null, store.Id);
                return RedirectToRoute("CheckoutPaymentMethod");
            }

            //check if pickup point is selected on the shipping address step
            if (!_orderSettings.DisplayPickupInStoreOnShippingMethodPage)
            {
                var selectedPickUpPoint = await _genericAttributeService
                    .GetAttributeAsync<PickupPoint>(customer, NopCustomerDefaults.SelectedPickupPointAttribute, store.Id);
                if (selectedPickUpPoint != null)
                    return RedirectToRoute("CheckoutPaymentMethod");
            }

            //model
            var model = await _checkoutModelFactory.PrepareShippingMethodModelAsync(cart, await _customerService.GetCustomerShippingAddressAsync(customer));

            if (_shippingSettings.BypassShippingMethodSelectionIfOnlyOne &&
                model.ShippingMethods.Count == 1)
            {
                //if we have only one shipping method, then a customer doesn't have to choose a shipping method
                await _genericAttributeService.SaveAttributeAsync(customer,
                    NopCustomerDefaults.SelectedShippingOptionAttribute,
                    model.ShippingMethods.First().ShippingOption,
                    store.Id);

                return RedirectToRoute("CheckoutPaymentMethod");
            }

            return View(model);
        }

        [HttpPost, ActionName("ShippingMethod")]
        [FormValueRequired("nextstep")]
        public virtual async Task<IActionResult> SelectShippingMethod(string shippingoption, IFormCollection form)
        {
            //validation
            if (_orderSettings.CheckoutDisabled)
                return RedirectToRoute("ShoppingCart");

            var customer = await _workContext.GetCurrentCustomerAsync();
            var store = await _storeContext.GetCurrentStoreAsync();
            var cart = await _shoppingCartService.GetShoppingCartAsync(customer, ShoppingCartType.ShoppingCart, store.Id);

            if (!cart.Any())
                return RedirectToRoute("ShoppingCart");

            if (_orderSettings.OnePageCheckoutEnabled)
                return RedirectToRoute("CheckoutOnePage");

            if (await _customerService.IsGuestAsync(customer) && !_orderSettings.AnonymousCheckoutAllowed)
                return Challenge();

            if (!await _shoppingCartService.ShoppingCartRequiresShippingAsync(cart))
            {
                await _genericAttributeService.SaveAttributeAsync<ShippingOption>(customer,
                    NopCustomerDefaults.SelectedShippingOptionAttribute, null, store.Id);
                return RedirectToRoute("CheckoutPaymentMethod");
            }

            //pickup point
            if (_shippingSettings.AllowPickupInStore && _orderSettings.DisplayPickupInStoreOnShippingMethodPage)
            {
                var pickupInStore = ParsePickupInStore(form);
                if (pickupInStore)
                {
                    var pickupOption = await ParsePickupOptionAsync(form);
                    await SavePickupOptionAsync(pickupOption);

                    return RedirectToRoute("CheckoutPaymentMethod");
                }

                //set value indicating that "pick up in store" option has not been chosen
                await _genericAttributeService.SaveAttributeAsync<PickupPoint>(customer, NopCustomerDefaults.SelectedPickupPointAttribute, null, store.Id);
            }

            //parse selected method 
            if (string.IsNullOrEmpty(shippingoption))
                return await ShippingMethod();
            var splittedOption = shippingoption.Split(new[] { "___" }, StringSplitOptions.RemoveEmptyEntries);
            if (splittedOption.Length != 2)
                return await ShippingMethod();
            var selectedName = splittedOption[0];
            var shippingRateComputationMethodSystemName = splittedOption[1];

            //find it
            //performance optimization. try cache first
            var shippingOptions = await _genericAttributeService.GetAttributeAsync<List<ShippingOption>>(customer,
                NopCustomerDefaults.OfferedShippingOptionsAttribute, store.Id);
            if (shippingOptions == null || !shippingOptions.Any())
            {
                //not found? let's load them using shipping service
                shippingOptions = (await _shippingService.GetShippingOptionsAsync(cart, await _customerService.GetCustomerShippingAddressAsync(customer),
                    customer, shippingRateComputationMethodSystemName, store.Id)).ShippingOptions.ToList();
            }
            else
            {
                //loaded cached results. let's filter result by a chosen shipping rate computation method
                shippingOptions = shippingOptions.Where(so => so.ShippingRateComputationMethodSystemName.Equals(shippingRateComputationMethodSystemName, StringComparison.InvariantCultureIgnoreCase))
                    .ToList();
            }

            var shippingOption = shippingOptions
                .Find(so => !string.IsNullOrEmpty(so.Name) && so.Name.Equals(selectedName, StringComparison.InvariantCultureIgnoreCase));
            if (shippingOption == null)
                return await ShippingMethod();

            //save
            await _genericAttributeService.SaveAttributeAsync(customer, NopCustomerDefaults.SelectedShippingOptionAttribute, shippingOption, store.Id);

            return RedirectToRoute("CheckoutPaymentMethod");
        }

        public virtual async Task<IActionResult> PaymentMethod()
        {
            //validation
            if (_orderSettings.CheckoutDisabled)
                return RedirectToRoute("ShoppingCart");

            var customer = await _workContext.GetCurrentCustomerAsync();
            var store = await _storeContext.GetCurrentStoreAsync();
            var cart = await _shoppingCartService.GetShoppingCartAsync(customer, ShoppingCartType.ShoppingCart, store.Id);

            if (!cart.Any())
                return RedirectToRoute("ShoppingCart");

            if (_orderSettings.OnePageCheckoutEnabled)
                return RedirectToRoute("CheckoutOnePage");

            if (await _customerService.IsGuestAsync(customer) && !_orderSettings.AnonymousCheckoutAllowed)
                return Challenge();

            //Check whether payment workflow is required
            //we ignore reward points during cart total calculation
            var isPaymentWorkflowRequired = await _orderProcessingService.IsPaymentWorkflowRequiredAsync(cart, false);
            if (!isPaymentWorkflowRequired)
            {
                await _genericAttributeService.SaveAttributeAsync<string>(customer,
                    NopCustomerDefaults.SelectedPaymentMethodAttribute, null, store.Id);
                return RedirectToRoute("CheckoutPaymentInfo");
            }

            //filter by country
            var filterByCountryId = 0;
            if (_addressSettings.CountryEnabled)
            {
                filterByCountryId = (await _customerService.GetCustomerBillingAddressAsync(customer))?.CountryId ?? 0;
            }

            //model
            var paymentMethodModel = await _checkoutModelFactory.PreparePaymentMethodModelAsync(cart, filterByCountryId);

            if (_paymentSettings.BypassPaymentMethodSelectionIfOnlyOne &&
                paymentMethodModel.PaymentMethods.Count == 1 && !paymentMethodModel.DisplayRewardPoints)
            {
                //if we have only one payment method and reward points are disabled or the current customer doesn't have any reward points
                //so customer doesn't have to choose a payment method

                await _genericAttributeService.SaveAttributeAsync(customer,
                    NopCustomerDefaults.SelectedPaymentMethodAttribute,
                    paymentMethodModel.PaymentMethods[0].PaymentMethodSystemName,
                    store.Id);
                return RedirectToRoute("CheckoutPaymentInfo");
            }

            return View(paymentMethodModel);
        }

        [HttpPost, ActionName("PaymentMethod")]
        [FormValueRequired("nextstep")]
        public virtual async Task<IActionResult> SelectPaymentMethod(string paymentmethod, CheckoutPaymentMethodModel model)
        {
            //validation
            if (_orderSettings.CheckoutDisabled)
                return RedirectToRoute("ShoppingCart");

            var customer = await _workContext.GetCurrentCustomerAsync();
            var store = await _storeContext.GetCurrentStoreAsync();
            var cart = await _shoppingCartService.GetShoppingCartAsync(customer, ShoppingCartType.ShoppingCart, store.Id);

            if (!cart.Any())
                return RedirectToRoute("ShoppingCart");

            if (_orderSettings.OnePageCheckoutEnabled)
                return RedirectToRoute("CheckoutOnePage");

            if (await _customerService.IsGuestAsync(customer) && !_orderSettings.AnonymousCheckoutAllowed)
                return Challenge();

            //reward points
            if (_rewardPointsSettings.Enabled)
            {
                await _genericAttributeService.SaveAttributeAsync(customer,
                    NopCustomerDefaults.UseRewardPointsDuringCheckoutAttribute, model.UseRewardPoints,
                    store.Id);
            }

            //Check whether payment workflow is required
            var isPaymentWorkflowRequired = await _orderProcessingService.IsPaymentWorkflowRequiredAsync(cart);
            if (!isPaymentWorkflowRequired)
            {
                await _genericAttributeService.SaveAttributeAsync<string>(customer,
                    NopCustomerDefaults.SelectedPaymentMethodAttribute, null, store.Id);
                return RedirectToRoute("CheckoutPaymentInfo");
            }
            //payment method 
            if (string.IsNullOrEmpty(paymentmethod))
                return await PaymentMethod();

            if (!await _paymentPluginManager.IsPluginActiveAsync(paymentmethod, customer, store.Id))
                return await PaymentMethod();

            //save
            await _genericAttributeService.SaveAttributeAsync(customer,
                NopCustomerDefaults.SelectedPaymentMethodAttribute, paymentmethod, store.Id);

            return RedirectToRoute("CheckoutPaymentInfo");
        }

        public virtual async Task<IActionResult> PaymentInfo()
        {
            //validation
            if (_orderSettings.CheckoutDisabled)
                return RedirectToRoute("ShoppingCart");

            var customer = await _workContext.GetCurrentCustomerAsync();
            var store = await _storeContext.GetCurrentStoreAsync();
            var cart = await _shoppingCartService.GetShoppingCartAsync(customer, ShoppingCartType.ShoppingCart, store.Id);

            if (!cart.Any())
                return RedirectToRoute("ShoppingCart");

            if (_orderSettings.OnePageCheckoutEnabled)
                return RedirectToRoute("CheckoutOnePage");

            if (await _customerService.IsGuestAsync(customer) && !_orderSettings.AnonymousCheckoutAllowed)
                return Challenge();

            //Check whether payment workflow is required
            var isPaymentWorkflowRequired = await _orderProcessingService.IsPaymentWorkflowRequiredAsync(cart);
            if (!isPaymentWorkflowRequired)
            {
                return RedirectToRoute("CheckoutConfirm");
            }

            //load payment method
            var paymentMethodSystemName = await _genericAttributeService.GetAttributeAsync<string>(customer,
                NopCustomerDefaults.SelectedPaymentMethodAttribute, store.Id);
            var paymentMethod = await _paymentPluginManager
                .LoadPluginBySystemNameAsync(paymentMethodSystemName, customer, store.Id);
            if (paymentMethod == null)
                return RedirectToRoute("CheckoutPaymentMethod");

            //Check whether payment info should be skipped
            if (paymentMethod.SkipPaymentInfo ||
                (paymentMethod.PaymentMethodType == PaymentMethodType.Redirection && _paymentSettings.SkipPaymentInfoStepForRedirectionPaymentMethods))
            {
                //skip payment info page
                var paymentInfo = new ProcessPaymentRequest();

                //session save
                HttpContext.Session.Set("OrderPaymentInfo", paymentInfo);

                return RedirectToRoute("CheckoutConfirm");
            }

            //model
            var model = await _checkoutModelFactory.PreparePaymentInfoModelAsync(paymentMethod);
            return View(model);
        }

        [HttpPost, ActionName("PaymentInfo")]
        [FormValueRequired("nextstep")]
        public virtual async Task<IActionResult> EnterPaymentInfo(IFormCollection form)
        {
            //validation
            if (_orderSettings.CheckoutDisabled)
                return RedirectToRoute("ShoppingCart");

            var customer = await _workContext.GetCurrentCustomerAsync();
            var store = await _storeContext.GetCurrentStoreAsync();
            var cart = await _shoppingCartService.GetShoppingCartAsync(customer, ShoppingCartType.ShoppingCart, store.Id);

            if (!cart.Any())
                return RedirectToRoute("ShoppingCart");

            if (_orderSettings.OnePageCheckoutEnabled)
                return RedirectToRoute("CheckoutOnePage");

            if (await _customerService.IsGuestAsync(customer) && !_orderSettings.AnonymousCheckoutAllowed)
                return Challenge();

            //Check whether payment workflow is required
            var isPaymentWorkflowRequired = await _orderProcessingService.IsPaymentWorkflowRequiredAsync(cart);
            if (!isPaymentWorkflowRequired)
            {
                return RedirectToRoute("CheckoutConfirm");
            }

            //load payment method
            var paymentMethodSystemName = await _genericAttributeService.GetAttributeAsync<string>(customer,
                NopCustomerDefaults.SelectedPaymentMethodAttribute, store.Id);
            var paymentMethod = await _paymentPluginManager
                .LoadPluginBySystemNameAsync(paymentMethodSystemName, customer, store.Id);
            if (paymentMethod == null)
                return RedirectToRoute("CheckoutPaymentMethod");

            var warnings = await paymentMethod.ValidatePaymentFormAsync(form);
            foreach (var warning in warnings)
                ModelState.AddModelError("", warning);
            if (ModelState.IsValid)
            {
                //get payment info
                var paymentInfo = await paymentMethod.GetPaymentInfoAsync(form);
                //set previous order GUID (if exists)
                _paymentService.GenerateOrderGuid(paymentInfo);

                //// Commented Aqib's implementation
                ///
                /*
                //stripe recurring logic
                //update customer, check if already created on stripe
                var existingCustomer = await _customerService.GetCustomerByEmailAsync(customer.Email);
                var createdStripeCustomer = new Stripe.Customer();
                if (existingCustomer != null && string.IsNullOrEmpty(existingCustomer.StripeCustomerId))
                {
                    var tokenCardOptions = new TokenCardOptions
                    {
                        Number = paymentInfo.CreditCardNumber,
                        ExpMonth = paymentInfo.CreditCardExpireMonth.ToString(),
                        ExpYear = paymentInfo.CreditCardExpireYear.ToString(),
                        Cvc = paymentInfo.CreditCardCvv2,
                    };

                    var token = await _productUnleashService.CreateStripeTokenForCard(tokenCardOptions);

                    var customerOption = new Stripe.CustomerCreateOptions
                    {
                        Name = customer.Username,
                        Email = customer.Email,
                        Source = token.Id
                    };

                    createdStripeCustomer = await _productUnleashService.CreateCustomerOnStripe(customerOption);
                    existingCustomer.StripeCustomerId = createdStripeCustomer.Id;
                    await _customerService.UpdateCustomerAsync(existingCustomer);
                    HttpContext.Session.Set("createdStripeCustomerId", createdStripeCustomer.Id);
                }

                //foreach (var item in cart)
                //{
                //    var existProduct = await _productService.GetProductByIdAsync(item.ProductId);

                //    if (!string.IsNullOrEmpty(existProduct.StripePriceId))
                //       await _productUnleashService.CreateSubscriptionOnStripe(createdStripeCustomer.Id, item.Quantity, existProduct.StripePriceId);
                //}
                */

                //session save
                HttpContext.Session.Set("OrderPaymentInfo", paymentInfo);
                return RedirectToRoute("CheckoutConfirm");
            }


            //If we got this far, something failed, redisplay form
            //model
            var model = await _checkoutModelFactory.PreparePaymentInfoModelAsync(paymentMethod);
            return View(model);
        }

        public virtual async Task<IActionResult> Confirm()
        {
            //validation
            if (_orderSettings.CheckoutDisabled)
                return RedirectToRoute("ShoppingCart");

            var customer = await _workContext.GetCurrentCustomerAsync();
            var store = await _storeContext.GetCurrentStoreAsync();
            var cart = await _shoppingCartService.GetShoppingCartAsync(customer, ShoppingCartType.ShoppingCart, store.Id);

            if (!cart.Any())
                return RedirectToRoute("ShoppingCart");

            if (_orderSettings.OnePageCheckoutEnabled)
                return RedirectToRoute("CheckoutOnePage");

            if (await _customerService.IsGuestAsync(customer) && !_orderSettings.AnonymousCheckoutAllowed)
                return Challenge();

            //model
            var model = await _checkoutModelFactory.PrepareConfirmOrderModelAsync(cart);
            return View(model);
        }

        [HttpPost, ActionName("Confirm")]
        public virtual async Task<IActionResult> ConfirmOrder()
        {
            //validation
            if (_orderSettings.CheckoutDisabled)
                return RedirectToRoute("ShoppingCart");

            var customer = await _workContext.GetCurrentCustomerAsync();
            var store = await _storeContext.GetCurrentStoreAsync();
            var cart = await _shoppingCartService.GetShoppingCartAsync(customer, ShoppingCartType.ShoppingCart, store.Id);
            var address = _addressService.GetCustomerAddressById(customer.Id);

            if (!cart.Any())
                return RedirectToRoute("ShoppingCart");

            if (_orderSettings.OnePageCheckoutEnabled)
                return RedirectToRoute("CheckoutOnePage");

            if (await _customerService.IsGuestAsync(customer) && !_orderSettings.AnonymousCheckoutAllowed)
                return Challenge();

            //model
            var model = await _checkoutModelFactory.PrepareConfirmOrderModelAsync(cart);
            try
            {
                //prevent 2 orders being placed within an X seconds time frame
                if (!await IsMinimumOrderPlacementIntervalValidAsync(customer))
                    throw new Exception(await _localizationService.GetResourceAsync("Checkout.MinOrderPlacementInterval"));

                //check stock availibility on unleash

                //// Commented Aqib's implementation

                /*var stockErrorMessage = string.Empty;
                foreach (var item in cart)
                {
                    var existingProduct = await _productService.GetProductByIdAsync(item.ProductId);
                    if (existingProduct.UnleashGuid != null)
                    {
                        var getStockOnHand = await _productUnleashService.GetStockOnHandByGuid(existingProduct.UnleashGuid);

                        if (getStockOnHand is not null && (item.Quantity > getStockOnHand.QtyOnHand))
                            stockErrorMessage += $"Sorry, available quantity of product {getStockOnHand.ProductCode} is {getStockOnHand.QtyOnHand}.\n";
                    }
                }

                if (!string.IsNullOrEmpty(stockErrorMessage))
                    throw new Exception(stockErrorMessage);
                */
                //place order
                var processPaymentRequest = HttpContext.Session.Get<ProcessPaymentRequest>("OrderPaymentInfo");
                if (processPaymentRequest == null)
                {
                    //Check whether payment workflow is required
                    if (await _orderProcessingService.IsPaymentWorkflowRequiredAsync(cart))
                        return RedirectToRoute("CheckoutPaymentInfo");

                    processPaymentRequest = new ProcessPaymentRequest();
                }
                _paymentService.GenerateOrderGuid(processPaymentRequest);
                processPaymentRequest.StoreId = store.Id;
                processPaymentRequest.CustomerId = customer.Id;
                processPaymentRequest.PaymentMethodSystemName = await _genericAttributeService.GetAttributeAsync<string>(customer,
                    NopCustomerDefaults.SelectedPaymentMethodAttribute, store.Id);
                HttpContext.Session.Set<ProcessPaymentRequest>("OrderPaymentInfo", processPaymentRequest);
                var placeOrderResult = await _orderProcessingService.PlaceOrderAsync(processPaymentRequest);
                if (placeOrderResult.Success)
                {
                    HttpContext.Session.Set<ProcessPaymentRequest>("OrderPaymentInfo", null);
                    var postProcessPaymentRequest = new PostProcessPaymentRequest
                    {
                        Order = placeOrderResult.PlacedOrder
                    };
                    await _paymentService.PostProcessPaymentAsync(postProcessPaymentRequest);

                    if (_webHelper.IsRequestBeingRedirected || _webHelper.IsPostBeingDone)
                    {
                        //redirection or POST has been done in PostProcessPayment
                        return Content(await _localizationService.GetResourceAsync("Checkout.RedirectMessage"));
                    }

                    DateTime dt = DateTime.Now; // replace with your DateTime value
                    long timestamp = (long)(dt - new DateTime(1970, 1, 1)).TotalMilliseconds;
                    string dateResult = "";

                    //create sales order on unleash
                    //// Commented Aqib's implementation
                    ///
                    /*
                        foreach (var item in cart)
                    {
                        var existingProduct = await _productService.GetProductByIdAsync(item.ProductId);
                        if (existingProduct != null)
                        {
                            existingProduct.StockQuantity = existingProduct.StockQuantity - item.Quantity;
                                await _productService.UpdateProductAsync(existingProduct);
                    var product = new SalesOrder.Product()
                    {
                        Guid = existingProduct.UnleashGuid,
                        ProductCode = existingProduct.Name
                    };

                    var salesOrderLines = new SalesOrder.SalesOrderLines()
                    {
                        LineNumber = 0,
                        LineType = null,
                        Product = product,
                        DueDate = dateResult,
                        OrderQuantity = item.Quantity,
                        UnitPrice = existingProduct.Price,
                        DiscountRate = 0,
                        LineTotal = 0,
                        Volume = null,
                        Weight = null,
                        Comments = "",
                        AverageLandedPriceAtTimeOfSale = 0,
                        TaxRate = 0,
                        LineTax = 0,
                        XeroTaxCode = null,
                        BCUnitPrice = 0,
                        BCLineTotal = 0,
                        BCLineTax = 0,
                        LineTaxCode = null,
                        XeroSalesAccount = null,
                        SerialNumbers = null,
                        BatchNumbers = null,
                        Guid = Guid.NewGuid().ToString(),
                        LastModifiedOn = dateResult
                    };

                    var discountUnitPrice = existingProduct.Price * (1 - salesOrderLines.DiscountRate);
                    salesOrderLines.LineTotal = discountUnitPrice * item.Quantity;

                    var customerDetail = new SalesOrder.Customer()
                    {
                        CustomerCode = customer.Username,
                        CustomerName = customer.Username,
                        CurrencyId = 0,
                        Guid = customer.CustomerGuid.ToString(),
                        LastModifiedOn = dateResult
                    };

                    SalesOrder.Warehouse warehouse = new SalesOrder.Warehouse();

                    var warehouseDetail = await _shippingService.GetWarehouseByIdAsync(existingProduct.WarehouseId);

                    if (warehouseDetail != null)
                    {
                        var addressDetail = await _addressService.GetAddressByIdAsync(warehouseDetail.AddressId);

                        if (warehouseDetail != null)
                        {
                            warehouse.WarehouseCode = "code";
                            warehouse.WarehouseName = "Name";
                            warehouse.IsDefault = false;
                            warehouse.StreetNo = "144";

                            if (addressDetail != null)
                            {
                                warehouse.AddressLine1 = "Address1";
                                warehouse.AddressLine2 = "Address2";
                                warehouse.Suburb = "suburb";
                                warehouse.City = "city";
                                warehouse.Region = "region";
                                warehouse.Country = "Country";
                                warehouse.PostCode = "Zip postal";
                                warehouse.PhoneNumber = "phone no";
                                warehouse.FaxNumber = "fax number";
                                warehouse.MobileNumber = "mobile no";
                                warehouse.DDINumber = "0";
                                warehouse.ContactName = "contact name";
                                warehouse.Obsolete = false;
                                warehouse.Guid = Guid.NewGuid().ToString();
                                warehouse.LastModifiedOn = dateResult;
                            }
                        }
                    }

                    var currency = new SalesOrder.Currency()
                    {
                        CurrencyCode = string.Empty,
                        Description = string.Empty,
                        Guid = Guid.NewGuid().ToString(),
                        LastModifiedOn = dateResult
                    };

                    var tax = new SalesOrder.Tax()
                    {
                        TaxCode = string.Empty,
                        Description = string.Empty,
                        TaxRate = 0,
                        CanApplyToExpenses = false,
                        CanApplyToRevenue = false,
                        Obsolete = false,
                        Guid = Guid.NewGuid().ToString(),
                        LastModifiedOn = null
                    };

                    var salesOrderRequest = new SalesOrderRequest();
                    salesOrderRequest.SalesOrderLines.Add(salesOrderLines);
                    salesOrderRequest.OrderNumber = "ON-1";
                    salesOrderRequest.OrderDate = dateResult;
                    salesOrderRequest.RequiredDate = dateResult;
                    salesOrderRequest.CompletedDate = null;
                    salesOrderRequest.OrderStatus = "Completed";
                    salesOrderRequest.Customer = customerDetail;
                    salesOrderRequest.CustomerRef = "CustomREf";
                    salesOrderRequest.Comments = "comments";
                    salesOrderRequest.Warehouse = null;
                    salesOrderRequest.ReceivedDate = null;
                    salesOrderRequest.DeliveryInstruction = "comments";
                    salesOrderRequest.DeliveryName = null;
                    salesOrderRequest.DeliveryStreetAddress = null;
                    salesOrderRequest.DeliverySuburb = null;
                    salesOrderRequest.DeliveryCity = null;
                    salesOrderRequest.DeliveryRegion = null;
                    salesOrderRequest.DeliveryCountry = null;
                    salesOrderRequest.DeliveryPostCode = null;
                    salesOrderRequest.Currency = null;
                    salesOrderRequest.ExchangeRate = 1;
                    salesOrderRequest.DiscountRate = 0;
                    salesOrderRequest.Tax = null;
                    salesOrderRequest.TaxRate = 0;
                    salesOrderRequest.XeroTaxCode = null;
                    salesOrderRequest.SubTotal = salesOrderLines.LineTotal;
                    salesOrderRequest.TaxTotal = 0;
                    salesOrderRequest.Total = salesOrderRequest.TaxTotal + salesOrderRequest.SubTotal;
                    salesOrderRequest.TotalVolume = 0;
                    salesOrderRequest.TotalWeight = existingProduct.Weight;
                    salesOrderRequest.BCSubTotal = 0;
                    salesOrderRequest.BCTaxTotal = 0;
                    salesOrderRequest.BCTotal = salesOrderRequest.TaxTotal + salesOrderRequest.SubTotal;
                    salesOrderRequest.PaymentDueDate = dateResult;
                    salesOrderRequest.AllocateProduct = true;
                    salesOrderRequest.SalesOrderGroup = null;
                    salesOrderRequest.DeliveryMethod = null;
                    salesOrderRequest.SalesPerson = null;
                    salesOrderRequest.SourceId = null;
                    salesOrderRequest.CreatedBy = string.Empty;
                    salesOrderRequest.CreatedOn = dateResult;
                    salesOrderRequest.LastModifiedBy = string.Empty;
                    salesOrderRequest.Guid = Guid.NewGuid().ToString();
                    salesOrderRequest.LastModifiedOn = dateResult;

                    if (existingProduct.UnleashGuid is not null)
                    {
                        var isProductExist = await _productUnleashService.GetProductByUnleashGuidAsync(existingProduct.UnleashGuid);

                        if (isProductExist is not null)
                            await _productUnleashService.CreateSalesOrderOnUnleash(salesOrderRequest);  //will return sales order id

                        //Todo: get sales order Id and save into order table in db
                        //salesOrderLines order should be save with respective order id.
                    }

                    var createdStripeCustomerId = HttpContext.Session.Get<string>("createdStripeCustomerId");
                    var existProduct = await _productService.GetProductByIdAsync(item.ProductId);

                    if (!string.IsNullOrEmpty(existProduct.StripePriceId))
                        await _productUnleashService.CreateSubscriptionOnStripe(createdStripeCustomerId, item.Quantity, existProduct.StripePriceId);
                }
            }
                     */

                    SalesOrder.Warehouse warehouse = null;

                    var salesOrderRequest = new SalesOrderRequest();
                    var guid = Guid.NewGuid().ToString();
                    int i = 1;
                    foreach (var item in cart)
                    {
                        var existingProduct = await _productService.GetProductByIdAsync(item.ProductId);
                        if (existingProduct != null)
                        {
                            var isProductExist = await _productUnleashService.GetProductByUnleashGuidAsync(existingProduct.UnleashGuid);
                            if (isProductExist != null)
                            {
                                
                                var product = new SalesOrder.Product()
                                {
                                    //Guid = existingProduct.UnleashGuid,
                                    ProductCode = existingProduct.UnleashGuid
                                };
                                var salesOrderLines = new SalesOrder.SalesOrderLines()
                                {
                                    LineNumber = i,
                                    LineType = null,
                                    Product = product,
                                    DueDate = dateResult,
                                    OrderQuantity = item.Quantity,
                                    UnitPrice = existingProduct.Price,
                                    DiscountRate = 0,
                                    LineTotal = 0,
                                    Volume = null,
                                    Weight = null,
                                    Comments = "",
                                    AverageLandedPriceAtTimeOfSale = 0,
                                    TaxRate = 0,
                                    LineTax = 0,
                                    XeroTaxCode = "NONE",
                                    BCUnitPrice = 0,
                                    BCLineTotal = 0,
                                    BCLineTax = 0,
                                    LineTaxCode = null,
                                    XeroSalesAccount = null,
                                    //SerialNumbers = null,
                                    BatchNumbers = null,
                                    Guid = guid,
                                    LastModifiedOn = dateResult
                                };

                                var discountUnitPrice = existingProduct.Price * (1 - salesOrderLines.DiscountRate);
                                salesOrderLines.LineTotal = discountUnitPrice * item.Quantity;

                                salesOrderRequest.SalesOrderLines.Add(salesOrderLines);
                            }

                        }
                        i++;
                    }
                    var customerDetail = new SalesOrder.Customer()
                    {
                        CustomerCode = UnleashAPI.GetCustomerCode(),
                        CustomerName = customer.Username,
                        CurrencyId = UnleashAPI.GetCustomerCurrencyId(),
                        Guid = customer.CustomerGuid.ToString(),
                        LastModifiedOn = dateResult
                        
                    };


                    var currency = new SalesOrder.Currency()
                    {
                        CurrencyCode = UnleashAPI.GetCurrencyCode(),
                        Description = UnleashAPI.GetCurrencyGuid(),
                        Guid = UnleashAPI.GetCurrencyGuid(),
                        LastModifiedOn = dateResult
                    };

                    var tax = new SalesOrder.Tax()
                    {
                        TaxCode = string.Empty,
                        Description = string.Empty,
                        TaxRate = 0,
                        CanApplyToExpenses = false,
                        CanApplyToRevenue = false,
                        Obsolete = false,
                        Guid = Guid.NewGuid().ToString(),
                        LastModifiedOn = null
                    };

                    var billingAddressId = customer.BillingAddressId ?? 0;
                    var billingAddress = address.Result.FirstOrDefault(x => x.Id == billingAddressId);

                    var country = await _countryService.GetCountryByIdAsync(billingAddress?.CountryId ?? 0);
                    var stateProvince = await _stateProvinceService.GetStateProvinceByIdAsync(billingAddress?.StateProvinceId ?? 0);

                    var shippingDetails = new SalesOrderRequest
                    {
                        DeliveryStreetAddress = billingAddress.Address1,
                        DeliveryStreetAddress2 = billingAddress.Address2,
                        DeliveryCity = billingAddress.City,
                        DeliveryCountry = country.Name,
                        DeliveryPostCode = billingAddress.ZipPostalCode,
                        DeliveryRegion = stateProvince.Name,
                        DeliveryName = billingAddress.FirstName + " " + billingAddress.LastName,
                    };

                    salesOrderRequest.OrderNumber = String.Concat("NOP-Order-", placeOrderResult.PlacedOrder.CustomOrderNumber);
                    salesOrderRequest.OrderDate = dateResult;
                    salesOrderRequest.RequiredDate = dateResult;
                    salesOrderRequest.CompletedDate = null;
                    salesOrderRequest.OrderStatus = "Placed";
                    salesOrderRequest.Customer = customerDetail;
                    salesOrderRequest.CustomerRef = "NOP-Order-" + placeOrderResult.PlacedOrder.CustomOrderNumber;
                    salesOrderRequest.Comments = "comments";
                    salesOrderRequest.Warehouse = null;
                    salesOrderRequest.ReceivedDate = null;
                    salesOrderRequest.DeliveryInstruction = "comments";
                    salesOrderRequest.DeliveryName = shippingDetails.DeliveryName;
                    salesOrderRequest.DeliveryStreetAddress = shippingDetails.DeliveryStreetAddress;
                    salesOrderRequest.DeliveryStreetAddress2 = shippingDetails?.DeliveryStreetAddress2;
                    salesOrderRequest.DeliverySuburb = null;
                    salesOrderRequest.DeliveryCity = shippingDetails.DeliveryCity;
                    salesOrderRequest.DeliveryRegion = shippingDetails.DeliveryRegion;
                    salesOrderRequest.DeliveryCountry = shippingDetails.DeliveryCountry;
                    salesOrderRequest.DeliveryPostCode = shippingDetails.DeliveryPostCode;
                    salesOrderRequest.Currency = currency;
                    salesOrderRequest.ExchangeRate = 1;
                    salesOrderRequest.DiscountRate = 0;
                    salesOrderRequest.Tax = null;
                    salesOrderRequest.TaxRate = 0;
                    salesOrderRequest.XeroTaxCode = "NONE";
                    salesOrderRequest.SubTotal = salesOrderRequest.SalesOrderLines.Sum(o => o.LineTotal);
                    salesOrderRequest.TaxTotal = 0;
                    salesOrderRequest.Total = salesOrderRequest.TaxTotal + salesOrderRequest.SubTotal;
                    salesOrderRequest.TotalVolume = 0;
                    salesOrderRequest.TotalWeight = 0.000M;
                    salesOrderRequest.BCSubTotal = 0;
                    salesOrderRequest.BCTaxTotal = 0;
                    salesOrderRequest.BCTotal = salesOrderRequest.TaxTotal + salesOrderRequest.SubTotal;
                    salesOrderRequest.PaymentDueDate = dateResult;
                    salesOrderRequest.AllocateProduct = true;
                    salesOrderRequest.SalesOrderGroup = null;
                    salesOrderRequest.DeliveryMethod = null;
                    salesOrderRequest.SalesPerson = null;
                    salesOrderRequest.SourceId = null;
                    salesOrderRequest.CreatedBy = string.Empty;
                    salesOrderRequest.CreatedOn = dateResult;
                    salesOrderRequest.LastModifiedBy = string.Empty;
                    salesOrderRequest.Guid = guid;
                    salesOrderRequest.LastModifiedOn = dateResult;

                    var response = await _productUnleashService.CreateSalesOrderOnUnleash(salesOrderRequest);  //will return sales order id
                    if (response != null)
                    {
                        var orderInfo = await _productUnleashService.UpdateSalesOrderGuidInOrder(response.Guid, placeOrderResult.PlacedOrder.Id);
                    }
                    else if (response == null)
                    {
                        var salesOrder = await _productUnleashService.GetUnleashSalesOrderByGuiIdAsync(guid);

                        if (salesOrder is not null)
                        {
                            var orderInfo = await _productUnleashService.UpdateSalesOrderGuidInOrder(salesOrder.Guid, placeOrderResult.PlacedOrder.Id);
                        }
                    }
                       

                    return RedirectToRoute("CheckoutCompleted", new { orderId = placeOrderResult.PlacedOrder.Id });
                }

                foreach (var error in placeOrderResult.Errors)
                    model.Warnings.Add(error);
            }
            catch (Exception exc)
            {
                await _logger.WarningAsync(exc.Message, exc);
                model.Warnings.Add(exc.Message);
            }

            //If we got this far, something failed, redisplay form
            return View(model);
        }

        #endregion

        #region Methods (one page checkout)

        protected virtual async Task<JsonResult> OpcLoadStepAfterShippingAddress(IList<ShoppingCartItem> cart)
        {
            var customer = await _workContext.GetCurrentCustomerAsync();
            var shippingMethodModel = await _checkoutModelFactory.PrepareShippingMethodModelAsync(cart, await _customerService.GetCustomerShippingAddressAsync(customer));
            if (_shippingSettings.BypassShippingMethodSelectionIfOnlyOne &&
                shippingMethodModel.ShippingMethods.Count == 1)
            {
                var store = await _storeContext.GetCurrentStoreAsync();
                //if we have only one shipping method, then a customer doesn't have to choose a shipping method
                await _genericAttributeService.SaveAttributeAsync(customer,
                    NopCustomerDefaults.SelectedShippingOptionAttribute,
                    shippingMethodModel.ShippingMethods.First().ShippingOption,
                    store.Id);

                //load next step
                return await OpcLoadStepAfterShippingMethod(cart);
            }

            return Json(new
            {
                update_section = new UpdateSectionJsonModel
                {
                    name = "shipping-method",
                    html = await RenderPartialViewToStringAsync("OpcShippingMethods", shippingMethodModel)
                },
                goto_section = "shipping_method"
            });
        }

        protected virtual async Task<JsonResult> OpcLoadStepAfterShippingMethod(IList<ShoppingCartItem> cart)
        {
            //Check whether payment workflow is required
            //we ignore reward points during cart total calculation
            var customer = await _workContext.GetCurrentCustomerAsync();
            var store = await _storeContext.GetCurrentStoreAsync();
            var isPaymentWorkflowRequired = await _orderProcessingService.IsPaymentWorkflowRequiredAsync(cart, false);
            if (isPaymentWorkflowRequired)
            {
                //filter by country
                var filterByCountryId = 0;
                if (_addressSettings.CountryEnabled)
                {
                    filterByCountryId = (await _customerService.GetCustomerBillingAddressAsync(customer))?.CountryId ?? 0;
                }

                //payment is required
                var paymentMethodModel = await _checkoutModelFactory.PreparePaymentMethodModelAsync(cart, filterByCountryId);

                if (_paymentSettings.BypassPaymentMethodSelectionIfOnlyOne &&
                    paymentMethodModel.PaymentMethods.Count == 1 && !paymentMethodModel.DisplayRewardPoints)
                {
                    //if we have only one payment method and reward points are disabled or the current customer doesn't have any reward points
                    //so customer doesn't have to choose a payment method

                    var selectedPaymentMethodSystemName = paymentMethodModel.PaymentMethods[0].PaymentMethodSystemName;
                    await _genericAttributeService.SaveAttributeAsync(customer,
                        NopCustomerDefaults.SelectedPaymentMethodAttribute,
                        selectedPaymentMethodSystemName, store.Id);

                    var paymentMethodInst = await _paymentPluginManager
                        .LoadPluginBySystemNameAsync(selectedPaymentMethodSystemName, customer, store.Id);
                    if (!_paymentPluginManager.IsPluginActive(paymentMethodInst))
                        throw new Exception("Selected payment method can't be parsed");

                    return await OpcLoadStepAfterPaymentMethod(paymentMethodInst, cart);
                }

                //customer have to choose a payment method
                return Json(new
                {
                    update_section = new UpdateSectionJsonModel
                    {
                        name = "payment-method",
                        html = await RenderPartialViewToStringAsync("OpcPaymentMethods", paymentMethodModel)
                    },
                    goto_section = "payment_method"
                });
            }

            //payment is not required
            await _genericAttributeService.SaveAttributeAsync<string>(customer,
                NopCustomerDefaults.SelectedPaymentMethodAttribute, null, store.Id);

            var confirmOrderModel = await _checkoutModelFactory.PrepareConfirmOrderModelAsync(cart);
            return Json(new
            {
                update_section = new UpdateSectionJsonModel
                {
                    name = "confirm-order",
                    html = await RenderPartialViewToStringAsync("OpcConfirmOrder", confirmOrderModel)
                },
                goto_section = "confirm_order"
            });
        }

        protected virtual async Task<JsonResult> OpcLoadStepAfterPaymentMethod(IPaymentMethod paymentMethod, IList<ShoppingCartItem> cart)
        {
            if (paymentMethod.SkipPaymentInfo ||
                (paymentMethod.PaymentMethodType == PaymentMethodType.Redirection && _paymentSettings.SkipPaymentInfoStepForRedirectionPaymentMethods))
            {
                //skip payment info page
                var paymentInfo = new ProcessPaymentRequest();

                //session save
                HttpContext.Session.Set("OrderPaymentInfo", paymentInfo);

                var confirmOrderModel = await _checkoutModelFactory.PrepareConfirmOrderModelAsync(cart);
                return Json(new
                {
                    update_section = new UpdateSectionJsonModel
                    {
                        name = "confirm-order",
                        html = await RenderPartialViewToStringAsync("OpcConfirmOrder", confirmOrderModel)
                    },
                    goto_section = "confirm_order"
                });
            }

            //return payment info page
            var paymenInfoModel = await _checkoutModelFactory.PreparePaymentInfoModelAsync(paymentMethod);
            return Json(new
            {
                update_section = new UpdateSectionJsonModel
                {
                    name = "payment-info",
                    html = await RenderPartialViewToStringAsync("OpcPaymentInfo", paymenInfoModel)
                },
                goto_section = "payment_info"
            });
        }

        public virtual async Task<IActionResult> OnePageCheckout()
        {
            //validation
            if (_orderSettings.CheckoutDisabled)
                return RedirectToRoute("ShoppingCart");

            var customer = await _workContext.GetCurrentCustomerAsync();
            var store = await _storeContext.GetCurrentStoreAsync();
            var cart = await _shoppingCartService.GetShoppingCartAsync(customer, ShoppingCartType.ShoppingCart, store.Id);

            if (!cart.Any())
                return RedirectToRoute("ShoppingCart");

            if (!_orderSettings.OnePageCheckoutEnabled)
                return RedirectToRoute("Checkout");

            if (await _customerService.IsGuestAsync(customer) && !_orderSettings.AnonymousCheckoutAllowed)
                return Challenge();

            var model = await _checkoutModelFactory.PrepareOnePageCheckoutModelAsync(cart);
            return View(model);
        }

        [HttpPost]
        public virtual async Task<IActionResult> OpcSaveBilling(CheckoutBillingAddressModel model, IFormCollection form)
        {
            try
            {
                //validation
                if (_orderSettings.CheckoutDisabled)
                    throw new Exception(await _localizationService.GetResourceAsync("Checkout.Disabled"));

                var customer = await _workContext.GetCurrentCustomerAsync();
                var store = await _storeContext.GetCurrentStoreAsync();
                var cart = await _shoppingCartService.GetShoppingCartAsync(customer, ShoppingCartType.ShoppingCart, store.Id);

                if (!cart.Any())
                    throw new Exception("Your cart is empty");

                if (!_orderSettings.OnePageCheckoutEnabled)
                    throw new Exception("One page checkout is disabled");

                if (await _customerService.IsGuestAsync(customer) && !_orderSettings.AnonymousCheckoutAllowed)
                    throw new Exception("Anonymous checkout is not allowed");

                _ = int.TryParse(form["billing_address_id"], out var billingAddressId);

                if (billingAddressId > 0)
                {
                    //existing address
                    var address = await _customerService.GetCustomerAddressAsync(customer.Id, billingAddressId)
                        ?? throw new Exception(await _localizationService.GetResourceAsync("Checkout.Address.NotFound"));

                    customer.BillingAddressId = address.Id;
                    await _customerService.UpdateCustomerAsync(customer);
                }
                else
                {
                    //new address
                    var newAddress = model.BillingNewAddress;

                    //custom address attributes
                    var customAttributes = await _addressAttributeParser.ParseCustomAddressAttributesAsync(form);
                    var customAttributeWarnings = await _addressAttributeParser.GetAttributeWarningsAsync(customAttributes);
                    foreach (var error in customAttributeWarnings)
                    {
                        ModelState.AddModelError("", error);
                    }

                    //validate model
                    if (!ModelState.IsValid)
                    {
                        //model is not valid. redisplay the form with errors
                        var billingAddressModel = await _checkoutModelFactory.PrepareBillingAddressModelAsync(cart,
                            selectedCountryId: newAddress.CountryId,
                            overrideAttributesXml: customAttributes);
                        billingAddressModel.NewAddressPreselected = true;
                        return Json(new
                        {
                            update_section = new UpdateSectionJsonModel
                            {
                                name = "billing",
                                html = await RenderPartialViewToStringAsync("OpcBillingAddress", billingAddressModel)
                            },
                            wrong_billing_address = true,
                        });
                    }

                    //try to find an address with the same values (don't duplicate records)
                    var address = _addressService.FindAddress((await _customerService.GetAddressesByCustomerIdAsync(customer.Id)).ToList(),
                        newAddress.FirstName, newAddress.LastName, newAddress.PhoneNumber,
                        newAddress.Email, newAddress.FaxNumber, newAddress.Company,
                        newAddress.Address1, newAddress.Address2, newAddress.City,
                        newAddress.County, newAddress.StateProvinceId, newAddress.ZipPostalCode,
                        newAddress.CountryId, customAttributes);

                    if (address == null)
                    {
                        //address is not found. let's create a new one
                        address = newAddress.ToEntity();
                        address.CustomAttributes = customAttributes;
                        address.CreatedOnUtc = DateTime.UtcNow;

                        //some validation
                        if (address.CountryId == 0)
                            address.CountryId = null;

                        if (address.StateProvinceId == 0)
                            address.StateProvinceId = null;

                        await _addressService.InsertAddressAsync(address);

                        await _customerService.InsertCustomerAddressAsync(customer, address);
                    }

                    customer.BillingAddressId = address.Id;

                    await _customerService.UpdateCustomerAsync(customer);
                }

                if (await _shoppingCartService.ShoppingCartRequiresShippingAsync(cart))
                {
                    //shipping is required
                    var address = await _customerService.GetCustomerBillingAddressAsync(customer);

                    //by default Shipping is available if the country is not specified
                    var shippingAllowed = !_addressSettings.CountryEnabled || ((await _countryService.GetCountryByAddressAsync(address))?.AllowsShipping ?? false);
                    if (_shippingSettings.ShipToSameAddress && model.ShipToSameAddress && shippingAllowed)
                    {
                        //ship to the same address
                        customer.ShippingAddressId = address.Id;
                        await _customerService.UpdateCustomerAsync(customer);
                        //reset selected shipping method (in case if "pick up in store" was selected)
                        await _genericAttributeService.SaveAttributeAsync<ShippingOption>(customer, NopCustomerDefaults.SelectedShippingOptionAttribute, null, store.Id);
                        await _genericAttributeService.SaveAttributeAsync<PickupPoint>(customer, NopCustomerDefaults.SelectedPickupPointAttribute, null, store.Id);
                        //limitation - "Ship to the same address" doesn't properly work in "pick up in store only" case (when no shipping plugins are available) 
                        return await OpcLoadStepAfterShippingAddress(cart);
                    }

                    //do not ship to the same address
                    var shippingAddressModel = await _checkoutModelFactory.PrepareShippingAddressModelAsync(cart, prePopulateNewAddressWithCustomerFields: true);

                    return Json(new
                    {
                        update_section = new UpdateSectionJsonModel
                        {
                            name = "shipping",
                            html = await RenderPartialViewToStringAsync("OpcShippingAddress", shippingAddressModel)
                        },
                        goto_section = "shipping"
                    });
                }

                //shipping is not required
                await _genericAttributeService.SaveAttributeAsync<ShippingOption>(customer, NopCustomerDefaults.SelectedShippingOptionAttribute, null, store.Id);

                //load next step
                return await OpcLoadStepAfterShippingMethod(cart);
            }
            catch (Exception exc)
            {
                await _logger.WarningAsync(exc.Message, exc, await _workContext.GetCurrentCustomerAsync());
                return Json(new { error = 1, message = exc.Message });
            }
        }

        [HttpPost]
        public virtual async Task<IActionResult> OpcSaveShipping(CheckoutShippingAddressModel model, IFormCollection form)
        {
            try
            {
                //validation
                if (_orderSettings.CheckoutDisabled)
                    throw new Exception(await _localizationService.GetResourceAsync("Checkout.Disabled"));

                var customer = await _workContext.GetCurrentCustomerAsync();
                var store = await _storeContext.GetCurrentStoreAsync();
                var cart = await _shoppingCartService.GetShoppingCartAsync(customer, ShoppingCartType.ShoppingCart, store.Id);

                if (!cart.Any())
                    throw new Exception("Your cart is empty");

                if (!_orderSettings.OnePageCheckoutEnabled)
                    throw new Exception("One page checkout is disabled");

                if (await _customerService.IsGuestAsync(customer) && !_orderSettings.AnonymousCheckoutAllowed)
                    throw new Exception("Anonymous checkout is not allowed");

                if (!await _shoppingCartService.ShoppingCartRequiresShippingAsync(cart))
                    throw new Exception("Shipping is not required");

                //pickup point
                if (_shippingSettings.AllowPickupInStore && !_orderSettings.DisplayPickupInStoreOnShippingMethodPage)
                {
                    var pickupInStore = ParsePickupInStore(form);
                    if (pickupInStore)
                    {
                        var pickupOption = await ParsePickupOptionAsync(form);
                        await SavePickupOptionAsync(pickupOption);

                        return await OpcLoadStepAfterShippingMethod(cart);
                    }

                    //set value indicating that "pick up in store" option has not been chosen
                    await _genericAttributeService.SaveAttributeAsync<PickupPoint>(customer, NopCustomerDefaults.SelectedPickupPointAttribute, null, store.Id);
                }

                _ = int.TryParse(form["shipping_address_id"], out var shippingAddressId);

                if (shippingAddressId > 0)
                {
                    //existing address
                    var address = await _customerService.GetCustomerAddressAsync(customer.Id, shippingAddressId)
                        ?? throw new Exception(await _localizationService.GetResourceAsync("Checkout.Address.NotFound"));

                    customer.ShippingAddressId = address.Id;
                    await _customerService.UpdateCustomerAsync(customer);
                }
                else
                {
                    //new address
                    var newAddress = model.ShippingNewAddress;

                    //custom address attributes
                    var customAttributes = await _addressAttributeParser.ParseCustomAddressAttributesAsync(form);
                    var customAttributeWarnings = await _addressAttributeParser.GetAttributeWarningsAsync(customAttributes);
                    foreach (var error in customAttributeWarnings)
                    {
                        ModelState.AddModelError("", error);
                    }

                    //validate model
                    if (!ModelState.IsValid)
                    {
                        //model is not valid. redisplay the form with errors
                        var shippingAddressModel = await _checkoutModelFactory.PrepareShippingAddressModelAsync(cart,
                            selectedCountryId: newAddress.CountryId,
                            overrideAttributesXml: customAttributes);
                        shippingAddressModel.NewAddressPreselected = true;
                        return Json(new
                        {
                            update_section = new UpdateSectionJsonModel
                            {
                                name = "shipping",
                                html = await RenderPartialViewToStringAsync("OpcShippingAddress", shippingAddressModel)
                            }
                        });
                    }

                    //try to find an address with the same values (don't duplicate records)
                    var address = _addressService.FindAddress((await _customerService.GetAddressesByCustomerIdAsync(customer.Id)).ToList(),
                        newAddress.FirstName, newAddress.LastName, newAddress.PhoneNumber,
                        newAddress.Email, newAddress.FaxNumber, newAddress.Company,
                        newAddress.Address1, newAddress.Address2, newAddress.City,
                        newAddress.County, newAddress.StateProvinceId, newAddress.ZipPostalCode,
                        newAddress.CountryId, customAttributes);

                    if (address == null)
                    {
                        address = newAddress.ToEntity();
                        address.CustomAttributes = customAttributes;
                        address.CreatedOnUtc = DateTime.UtcNow;

                        await _addressService.InsertAddressAsync(address);

                        await _customerService.InsertCustomerAddressAsync(customer, address);
                    }

                    customer.ShippingAddressId = address.Id;

                    await _customerService.UpdateCustomerAsync(customer);
                }

                return await OpcLoadStepAfterShippingAddress(cart);
            }
            catch (Exception exc)
            {
                await _logger.WarningAsync(exc.Message, exc, await _workContext.GetCurrentCustomerAsync());
                return Json(new { error = 1, message = exc.Message });
            }
        }

        [HttpPost]
        public virtual async Task<IActionResult> OpcSaveShippingMethod(string shippingoption, IFormCollection form)
        {
            try
            {
                //validation
                if (_orderSettings.CheckoutDisabled)
                    throw new Exception(await _localizationService.GetResourceAsync("Checkout.Disabled"));

                var customer = await _workContext.GetCurrentCustomerAsync();
                var store = await _storeContext.GetCurrentStoreAsync();
                var cart = await _shoppingCartService.GetShoppingCartAsync(customer, ShoppingCartType.ShoppingCart, store.Id);

                if (!cart.Any())
                    throw new Exception("Your cart is empty");

                if (!_orderSettings.OnePageCheckoutEnabled)
                    throw new Exception("One page checkout is disabled");

                if (await _customerService.IsGuestAsync(customer) && !_orderSettings.AnonymousCheckoutAllowed)
                    throw new Exception("Anonymous checkout is not allowed");

                if (!await _shoppingCartService.ShoppingCartRequiresShippingAsync(cart))
                    throw new Exception("Shipping is not required");

                //pickup point
                if (_shippingSettings.AllowPickupInStore && _orderSettings.DisplayPickupInStoreOnShippingMethodPage)
                {
                    var pickupInStore = ParsePickupInStore(form);
                    if (pickupInStore)
                    {
                        var pickupOption = await ParsePickupOptionAsync(form);
                        await SavePickupOptionAsync(pickupOption);

                        return await OpcLoadStepAfterShippingMethod(cart);
                    }

                    //set value indicating that "pick up in store" option has not been chosen
                    await _genericAttributeService.SaveAttributeAsync<PickupPoint>(customer, NopCustomerDefaults.SelectedPickupPointAttribute, null, store.Id);
                }

                //parse selected method 
                if (string.IsNullOrEmpty(shippingoption))
                    throw new Exception("Selected shipping method can't be parsed");
                var splittedOption = shippingoption.Split(new[] { "___" }, StringSplitOptions.RemoveEmptyEntries);
                if (splittedOption.Length != 2)
                    throw new Exception("Selected shipping method can't be parsed");
                var selectedName = splittedOption[0];
                var shippingRateComputationMethodSystemName = splittedOption[1];

                //find it
                //performance optimization. try cache first
                var shippingOptions = await _genericAttributeService.GetAttributeAsync<List<ShippingOption>>(customer,
                    NopCustomerDefaults.OfferedShippingOptionsAttribute, store.Id);
                if (shippingOptions == null || !shippingOptions.Any())
                {
                    //not found? let's load them using shipping service
                    shippingOptions = (await _shippingService.GetShippingOptionsAsync(cart, await _customerService.GetCustomerShippingAddressAsync(customer),
                        customer, shippingRateComputationMethodSystemName, store.Id)).ShippingOptions.ToList();
                }
                else
                {
                    //loaded cached results. let's filter result by a chosen shipping rate computation method
                    shippingOptions = shippingOptions.Where(so => so.ShippingRateComputationMethodSystemName.Equals(shippingRateComputationMethodSystemName, StringComparison.InvariantCultureIgnoreCase))
                        .ToList();
                }

                var shippingOption = shippingOptions
                    .Find(so => !string.IsNullOrEmpty(so.Name) && so.Name.Equals(selectedName, StringComparison.InvariantCultureIgnoreCase));
                if (shippingOption == null)
                    throw new Exception("Selected shipping method can't be loaded");

                //save
                await _genericAttributeService.SaveAttributeAsync(customer, NopCustomerDefaults.SelectedShippingOptionAttribute, shippingOption, store.Id);

                //load next step
                return await OpcLoadStepAfterShippingMethod(cart);
            }
            catch (Exception exc)
            {
                await _logger.WarningAsync(exc.Message, exc, await _workContext.GetCurrentCustomerAsync());
                return Json(new { error = 1, message = exc.Message });
            }
        }

        [HttpPost]
        public virtual async Task<IActionResult> OpcSavePaymentMethod(string paymentmethod, CheckoutPaymentMethodModel model)
        {
            try
            {
                //validation
                if (_orderSettings.CheckoutDisabled)
                    throw new Exception(await _localizationService.GetResourceAsync("Checkout.Disabled"));

                var customer = await _workContext.GetCurrentCustomerAsync();
                var store = await _storeContext.GetCurrentStoreAsync();
                var cart = await _shoppingCartService.GetShoppingCartAsync(customer, ShoppingCartType.ShoppingCart, store.Id);

                if (!cart.Any())
                    throw new Exception("Your cart is empty");

                if (!_orderSettings.OnePageCheckoutEnabled)
                    throw new Exception("One page checkout is disabled");

                if (await _customerService.IsGuestAsync(customer) && !_orderSettings.AnonymousCheckoutAllowed)
                    throw new Exception("Anonymous checkout is not allowed");

                //payment method 
                if (string.IsNullOrEmpty(paymentmethod))
                    throw new Exception("Selected payment method can't be parsed");

                //reward points
                if (_rewardPointsSettings.Enabled)
                {
                    await _genericAttributeService.SaveAttributeAsync(customer,
                        NopCustomerDefaults.UseRewardPointsDuringCheckoutAttribute, model.UseRewardPoints,
                        store.Id);
                }

                //Check whether payment workflow is required
                var isPaymentWorkflowRequired = await _orderProcessingService.IsPaymentWorkflowRequiredAsync(cart);
                if (!isPaymentWorkflowRequired)
                {
                    //payment is not required
                    await _genericAttributeService.SaveAttributeAsync<string>(customer,
                        NopCustomerDefaults.SelectedPaymentMethodAttribute, null, store.Id);

                    var confirmOrderModel = await _checkoutModelFactory.PrepareConfirmOrderModelAsync(cart);
                    return Json(new
                    {
                        update_section = new UpdateSectionJsonModel
                        {
                            name = "confirm-order",
                            html = await RenderPartialViewToStringAsync("OpcConfirmOrder", confirmOrderModel)
                        },
                        goto_section = "confirm_order"
                    });
                }

                var paymentMethodInst = await _paymentPluginManager
                    .LoadPluginBySystemNameAsync(paymentmethod, customer, store.Id);
                if (!_paymentPluginManager.IsPluginActive(paymentMethodInst))
                    throw new Exception("Selected payment method can't be parsed");

                //save
                await _genericAttributeService.SaveAttributeAsync(customer,
                    NopCustomerDefaults.SelectedPaymentMethodAttribute, paymentmethod, store.Id);

                return await OpcLoadStepAfterPaymentMethod(paymentMethodInst, cart);
            }
            catch (Exception exc)
            {
                await _logger.WarningAsync(exc.Message, exc, await _workContext.GetCurrentCustomerAsync());
                return Json(new { error = 1, message = exc.Message });
            }
        }

        [HttpPost]
        [IgnoreAntiforgeryToken]
        public virtual async Task<IActionResult> OpcSavePaymentInfo(IFormCollection form)
        {
            try
            {
                //validation
                if (_orderSettings.CheckoutDisabled)
                    throw new Exception(await _localizationService.GetResourceAsync("Checkout.Disabled"));

                var customer = await _workContext.GetCurrentCustomerAsync();
                var store = await _storeContext.GetCurrentStoreAsync();
                var cart = await _shoppingCartService.GetShoppingCartAsync(customer, ShoppingCartType.ShoppingCart, store.Id);

                if (!cart.Any())
                    throw new Exception("Your cart is empty");

                if (!_orderSettings.OnePageCheckoutEnabled)
                    throw new Exception("One page checkout is disabled");

                if (await _customerService.IsGuestAsync(customer) && !_orderSettings.AnonymousCheckoutAllowed)
                    throw new Exception("Anonymous checkout is not allowed");

                var paymentMethodSystemName = await _genericAttributeService.GetAttributeAsync<string>(customer,
                    NopCustomerDefaults.SelectedPaymentMethodAttribute, store.Id);
                var paymentMethod = await _paymentPluginManager
                    .LoadPluginBySystemNameAsync(paymentMethodSystemName, customer, store.Id)
                    ?? throw new Exception("Payment method is not selected");

                var warnings = await paymentMethod.ValidatePaymentFormAsync(form);
                foreach (var warning in warnings)
                    ModelState.AddModelError("", warning);
                if (ModelState.IsValid)
                {
                    //get payment info
                    var paymentInfo = await paymentMethod.GetPaymentInfoAsync(form);
                    //set previous order GUID (if exists)
                    _paymentService.GenerateOrderGuid(paymentInfo);

                    //session save
                    HttpContext.Session.Set("OrderPaymentInfo", paymentInfo);

                    var confirmOrderModel = await _checkoutModelFactory.PrepareConfirmOrderModelAsync(cart);
                    return Json(new
                    {
                        update_section = new UpdateSectionJsonModel
                        {
                            name = "confirm-order",
                            html = await RenderPartialViewToStringAsync("OpcConfirmOrder", confirmOrderModel)
                        },
                        goto_section = "confirm_order"
                    });
                }

                //If we got this far, something failed, redisplay form
                var paymenInfoModel = await _checkoutModelFactory.PreparePaymentInfoModelAsync(paymentMethod);
                return Json(new
                {
                    update_section = new UpdateSectionJsonModel
                    {
                        name = "payment-info",
                        html = await RenderPartialViewToStringAsync("OpcPaymentInfo", paymenInfoModel)
                    }
                });
            }
            catch (Exception exc)
            {
                await _logger.WarningAsync(exc.Message, exc, await _workContext.GetCurrentCustomerAsync());
                return Json(new { error = 1, message = exc.Message });
            }
        }

        [HttpPost]
        public virtual async Task<IActionResult> OpcConfirmOrder()
        {
            try
            {
                //validation
                if (_orderSettings.CheckoutDisabled)
                    throw new Exception(await _localizationService.GetResourceAsync("Checkout.Disabled"));

                var customer = await _workContext.GetCurrentCustomerAsync();
                var store = await _storeContext.GetCurrentStoreAsync();
                var cart = await _shoppingCartService.GetShoppingCartAsync(customer, ShoppingCartType.ShoppingCart, store.Id);

                if (!cart.Any())
                    throw new Exception("Your cart is empty");

                if (!_orderSettings.OnePageCheckoutEnabled)
                    throw new Exception("One page checkout is disabled");

                if (await _customerService.IsGuestAsync(customer) && !_orderSettings.AnonymousCheckoutAllowed)
                    throw new Exception("Anonymous checkout is not allowed");

                //prevent 2 orders being placed within an X seconds time frame
                if (!await IsMinimumOrderPlacementIntervalValidAsync(customer))
                    throw new Exception(await _localizationService.GetResourceAsync("Checkout.MinOrderPlacementInterval"));

                //place order
                var processPaymentRequest = HttpContext.Session.Get<ProcessPaymentRequest>("OrderPaymentInfo");
                if (processPaymentRequest == null)
                {
                    //Check whether payment workflow is required
                    if (await _orderProcessingService.IsPaymentWorkflowRequiredAsync(cart))
                    {
                        throw new Exception("Payment information is not entered");
                    }

                    processPaymentRequest = new ProcessPaymentRequest();
                }
                _paymentService.GenerateOrderGuid(processPaymentRequest);
                processPaymentRequest.StoreId = store.Id;
                processPaymentRequest.CustomerId = customer.Id;
                processPaymentRequest.PaymentMethodSystemName = await _genericAttributeService.GetAttributeAsync<string>(customer,
                    NopCustomerDefaults.SelectedPaymentMethodAttribute, store.Id);
                HttpContext.Session.Set<ProcessPaymentRequest>("OrderPaymentInfo", processPaymentRequest);
                var placeOrderResult = await _orderProcessingService.PlaceOrderAsync(processPaymentRequest);
                if (placeOrderResult.Success)
                {
                    HttpContext.Session.Set<ProcessPaymentRequest>("OrderPaymentInfo", null);
                    var postProcessPaymentRequest = new PostProcessPaymentRequest
                    {
                        Order = placeOrderResult.PlacedOrder
                    };

                    var paymentMethod = await _paymentPluginManager
                        .LoadPluginBySystemNameAsync(placeOrderResult.PlacedOrder.PaymentMethodSystemName, customer, store.Id);
                    if (paymentMethod == null)
                        //payment method could be null if order total is 0
                        //success
                        return Json(new { success = 1 });

                    if (paymentMethod.PaymentMethodType == PaymentMethodType.Redirection)
                    {
                        //Redirection will not work because it's AJAX request.
                        //That's why we don't process it here (we redirect a user to another page where he'll be redirected)

                        //redirect
                        return Json(new
                        {
                            redirect = $"{_webHelper.GetStoreLocation()}checkout/OpcCompleteRedirectionPayment"
                        });
                    }

                    await _paymentService.PostProcessPaymentAsync(postProcessPaymentRequest);
                    //success
                    return Json(new { success = 1 });
                }

                //error
                var confirmOrderModel = new CheckoutConfirmModel();
                foreach (var error in placeOrderResult.Errors)
                    confirmOrderModel.Warnings.Add(error);

                return Json(new
                {
                    update_section = new UpdateSectionJsonModel
                    {
                        name = "confirm-order",
                        html = await RenderPartialViewToStringAsync("OpcConfirmOrder", confirmOrderModel)
                    },
                    goto_section = "confirm_order"
                });
            }
            catch (Exception exc)
            {
                await _logger.WarningAsync(exc.Message, exc, await _workContext.GetCurrentCustomerAsync());
                return Json(new { error = 1, message = exc.Message });
            }
        }

        public virtual async Task<IActionResult> OpcCompleteRedirectionPayment()
        {
            try
            {
                //validation
                if (!_orderSettings.OnePageCheckoutEnabled)
                    return RedirectToRoute("Homepage");

                var customer = await _workContext.GetCurrentCustomerAsync();
                if (await _customerService.IsGuestAsync(customer) && !_orderSettings.AnonymousCheckoutAllowed)
                    return Challenge();

                //get the order
                var store = await _storeContext.GetCurrentStoreAsync();
                var order = (await _orderService.SearchOrdersAsync(storeId: store.Id,
                customerId: customer.Id, pageSize: 1)).FirstOrDefault();
                if (order == null)
                    return RedirectToRoute("Homepage");

                var paymentMethod = await _paymentPluginManager
                    .LoadPluginBySystemNameAsync(order.PaymentMethodSystemName, customer, store.Id);
                if (paymentMethod == null)
                    return RedirectToRoute("Homepage");
                if (paymentMethod.PaymentMethodType != PaymentMethodType.Redirection)
                    return RedirectToRoute("Homepage");

                //ensure that order has been just placed
                if ((DateTime.UtcNow - order.CreatedOnUtc).TotalMinutes > 3)
                    return RedirectToRoute("Homepage");

                //Redirection will not work on one page checkout page because it's AJAX request.
                //That's why we process it here
                var postProcessPaymentRequest = new PostProcessPaymentRequest
                {
                    Order = order
                };

                await _paymentService.PostProcessPaymentAsync(postProcessPaymentRequest);

                if (_webHelper.IsRequestBeingRedirected || _webHelper.IsPostBeingDone)
                {
                    //redirection or POST has been done in PostProcessPayment
                    return Content(await _localizationService.GetResourceAsync("Checkout.RedirectMessage"));
                }

                //if no redirection has been done (to a third-party payment page)
                //theoretically it's not possible
                return RedirectToRoute("CheckoutCompleted", new { orderId = order.Id });
            }
            catch (Exception exc)
            {
                await _logger.WarningAsync(exc.Message, exc, await _workContext.GetCurrentCustomerAsync());
                return Content(exc.Message);
            }
        }

        #endregion
    }
}
