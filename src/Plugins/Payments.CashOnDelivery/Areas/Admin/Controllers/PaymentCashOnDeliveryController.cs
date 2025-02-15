﻿using Grand.Business.Core.Interfaces.Common.Configuration;
using Grand.Business.Core.Interfaces.Common.Localization;
using Grand.Business.Core.Interfaces.Common.Stores;
using Grand.Domain.Permissions;
using Grand.Domain.Common;
using Grand.Domain.Customers;
using Grand.Infrastructure;
using Grand.Web.Common.Controllers;
using Grand.Web.Common.Filters;
using Grand.Web.Common.Security.Authorization;
using Microsoft.AspNetCore.Mvc;
using Payments.CashOnDelivery.Models;

namespace Payments.CashOnDelivery.Areas.Admin.Controllers;

[AuthorizeAdmin]
[Area("Admin")]
[PermissionAuthorize(PermissionSystemName.PaymentMethods)]
public class PaymentCashOnDeliveryController : BasePaymentController
{
    private readonly ISettingService _settingService;
    private readonly IStoreService _storeService;
    private readonly ITranslationService _translationService;
    private readonly IWorkContextAccessor _workContextAccessor;


    public PaymentCashOnDeliveryController(IWorkContextAccessor workContextAccessor,
        IStoreService storeService,
        ISettingService settingService,
        ITranslationService translationService)
    {
        _workContextAccessor = workContextAccessor;
        _storeService = storeService;
        _settingService = settingService;
        _translationService = translationService;
    }


    protected virtual async Task<string> GetActiveStore(IStoreService storeService, IWorkContext workContext)
    {
        var stores = await storeService.GetAllStores();
        if (stores.Count < 2)
            return stores.FirstOrDefault().Id;

        var storeId =
            workContext.CurrentCustomer.GetUserFieldFromEntity<string>(SystemCustomerFieldNames
                .AdminAreaStoreScopeConfiguration);
        var store = await storeService.GetStoreById(storeId);

        return store != null ? store.Id : "";
    }

    public async Task<IActionResult> Configure()
    {
        //load settings for a chosen store scope
        var storeScope = await GetActiveStore(_storeService, _workContextAccessor.WorkContext);
        var cashOnDeliveryPaymentSettings = await _settingService.LoadSetting<CashOnDeliveryPaymentSettings>(storeScope);

        var model = new ConfigurationModel {
            DescriptionText = cashOnDeliveryPaymentSettings.DescriptionText,
            AdditionalFee = cashOnDeliveryPaymentSettings.AdditionalFee,
            AdditionalFeePercentage = cashOnDeliveryPaymentSettings.AdditionalFeePercentage,
            ShippableProductRequired = cashOnDeliveryPaymentSettings.ShippableProductRequired,
            DisplayOrder = cashOnDeliveryPaymentSettings.DisplayOrder,
            SkipPaymentInfo = cashOnDeliveryPaymentSettings.SkipPaymentInfo
        };
        model.DescriptionText = cashOnDeliveryPaymentSettings.DescriptionText;

        model.ActiveStore = storeScope;

        return View(model);
    }

    [HttpPost]
    public async Task<IActionResult> Configure(ConfigurationModel model)
    {
        if (!ModelState.IsValid)
            return await Configure();

        //load settings for a chosen store scope
        var storeScope = await GetActiveStore(_storeService, _workContextAccessor.WorkContext);
        var cashOnDeliveryPaymentSettings = await _settingService.LoadSetting<CashOnDeliveryPaymentSettings>(storeScope);

        //save settings
        cashOnDeliveryPaymentSettings.DescriptionText = model.DescriptionText;
        cashOnDeliveryPaymentSettings.AdditionalFee = model.AdditionalFee;
        cashOnDeliveryPaymentSettings.AdditionalFeePercentage = model.AdditionalFeePercentage;
        cashOnDeliveryPaymentSettings.ShippableProductRequired = model.ShippableProductRequired;
        cashOnDeliveryPaymentSettings.DisplayOrder = model.DisplayOrder;
        cashOnDeliveryPaymentSettings.SkipPaymentInfo = model.SkipPaymentInfo;

        await _settingService.SaveSetting(cashOnDeliveryPaymentSettings, storeScope);

        //now clear settings cache
        await _settingService.ClearCache();

        Success(_translationService.GetResource("Admin.Plugins.Saved"));

        return await Configure();
    }
}