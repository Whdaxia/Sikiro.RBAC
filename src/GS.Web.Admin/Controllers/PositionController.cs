﻿using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Sikiro.Entity.System;
using Sikiro.Nosql.Mongo;
using Sikiro.Service.System;
using Sikiro.Tookits.Base;
using Sikiro.Tookits.Extension;
using Sikiro.Tookits.Helper;
using Sikiro.Web.Admin.Models.Position;
using Sikiro.Web.Admin.Permission;

namespace Sikiro.Web.Admin.Controllers
{
    /// <summary>
    /// 岗位管理
    /// </summary>
    public class PositionController : BaseController
    {
        #region 初始化
        private readonly PositionService _positionService;
        private readonly AdministratorService _administratorService;
        public PositionController(PositionService positionService, AdministratorService administratorService)
        {
            _positionService = positionService;
            _administratorService = administratorService;
        }
        #endregion

        #region 列表
        public IActionResult Index()
        {
            return View();
        }

        [Permission(PermCode.Position_List)]
        public IActionResult List(PageListParams<PositionParams> model)
        {
            var where = ExpressionBuilder.Init<Position>();
            var param = model.Params;

            if (!param.PositionName.IsNullOrEmpty())
                where = where.And(a => a.Name.Contains(param.PositionName));
            var positionList = _positionService.GetPageList(model.Page, model.Limit, where);
            var dministratorList = _administratorService.GetListByIds(positionList.Items.Select(a => a.AdministratorId).ToList());
            var result = _positionService.GetPageList(model.Page, model.Limit, where).UpdateForPageListResult(a =>
            {
                var dministrator = dministratorList.FirstOrDefault(b => b.Id == a.AdministratorId);

                return new PositionModel
                {
                    Id = a.Id.ToString(),
                    Name = a.Name,
                    Remark = a.Remark ?? "",
                    UpdateDateTime = a.UpdateDateTime,
                    AdministratorName = dministrator == null ? "" : dministrator.RealName
                };
            });
            return PageList(result);
        }
        #endregion

        #region 新增编辑
        public IActionResult AddEdit(string id)
        {
            if (string.IsNullOrEmpty(id))
                return View();

            var model = _positionService.Get(c => c.Id == id.ToObjectId());
            var result = model.MapTo<AddEditPosition>();
            result.Id = model.Id.ToString();
            return View(result);
        }

        [Permission(PermCode.Position_Edit)]
        [Permission(PermCode.Position_Add)]
        [HttpPost]
        public IActionResult AddEdit(AddEditPosition model)
        {
            ServiceResult result;
            if (!string.IsNullOrEmpty(model.Id))
            {
                var comModel = _positionService.Get(c => c.Name == model.Name);
                if (comModel != null && comModel.Id != model.Id.ToObjectId())
                    return Json(ServiceResult.IsFailed("岗位名已存在"));

                var entity = model.MapTo<Position>();
                entity.UpdateDateTime = DateTime.Now;
                entity.AdministratorId = CurrentUserData.UserId.ToObjectId();
                result = _positionService.Update(entity.Id.ToString(), entity);
            }
            else
            {
                var isExist = _positionService.Exists(c => c.Name == model.Name);
                if (isExist)
                    return Json(ServiceResult.IsFailed("岗位名已存在"));

                var entity = model.MapTo<Position>();
                entity.UpdateDateTime = DateTime.Now;
                entity.AdministratorId = CurrentUserData.UserId.ToObjectId();
                result = _positionService.Add(entity);

            }

            return Json(result);
        }
        #endregion

        #region 下拉框
        /// <summary>
        /// 岗位下拉框
        /// </summary>
        /// <returns></returns>
        public IActionResult PositionSelect(List<string> value)
        {
            var selectList = _positionService.GetSelectList().Select(a => new SelectListItem
            {
                Value = a.Id.ToString(),
                Text = a.Name,
                Selected = value.Contains(a.Id.ToString())
            });

            return Json(selectList);
        }
        #endregion

        #region 删除
        [Permission(PermCode.Position_Delete)]
        [HttpPost]
        public IActionResult Delete(string id)
        {
            var model = _administratorService.GetByPositionId(id.ToObjectId());
            if (model != null)
            {
                return Json(ServiceResult.IsFailed("已有其他人员分配该岗位,不能删除"));
            }
            var json = _positionService.Delete(c => c.Id == id.ToObjectId());
            return Json(json);
        }
        #endregion
    }
}