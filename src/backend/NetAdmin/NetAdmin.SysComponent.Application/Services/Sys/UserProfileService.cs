using NetAdmin.Domain.Contexts;
using NetAdmin.Domain.DbMaps.Sys;
using NetAdmin.Domain.Dto.Sys.Dic.Content;
using NetAdmin.Domain.Dto.Sys.UserProfile;
using NetAdmin.Domain.Extensions;

namespace NetAdmin.SysComponent.Application.Services.Sys;

/// <inheritdoc cref="IUserProfileService" />
public sealed class UserProfileService(BasicRepository<Sys_UserProfile, long> rpo) //
    : RepositoryService<Sys_UserProfile, long, IUserProfileService>(rpo), IUserProfileService
{
    /// <summary>
    ///     构建应用配置
    /// </summary>
    public static string BuildAppConfig(Dictionary<long, string> roles)
    {
        try {
            return new string[][] { [
                                          Chars.FLG_FRONT_APP_SET_HOME_GRID
                                        , new { content = roles.MaxBy(x => x.Key).Value.ToObject<object>(), datetime = 0 }.ToJson()
                                      ]
                                  }.ToJson();
        }
        catch {
            return "[]";
        }
    }

    /// <inheritdoc />
    public async Task<int> BulkDeleteAsync(BulkReq<DelReq> req)
    {
        req.ThrowIfInvalid();
        var ret = 0;

        // ReSharper disable once LoopCanBeConvertedToQuery
        foreach (var item in req.Items) {
            ret += await DeleteAsync(item).ConfigureAwait(false);
        }

        return ret;
    }

    /// <inheritdoc />
    public Task<long> CountAsync(QueryReq<QueryUserProfileReq> req)
    {
        req.ThrowIfInvalid();
        return QueryInternalComplex(req).WithNoLockNoWait().CountAsync();
    }

    /// <inheritdoc />
    public async Task<IOrderedEnumerable<KeyValuePair<IImmutableDictionary<string, string>, int>>> CountByAsync(QueryReq<QueryUserProfileReq> req)
    {
        req.ThrowIfInvalid();
        var ret = await QueryInternal(req with { Order = Orders.None })
                        .WithNoLockNoWait()
                        .GroupBy(req.GetToListExp<Sys_UserProfile>())
                        .ToDictionaryAsync(a => a.Count())
                        .ConfigureAwait(false);
        return ret.Select(x => new KeyValuePair<IImmutableDictionary<string, string>, int>(
                              req.RequiredFields.ToImmutableDictionary(
                                  y => y, y => typeof(Sys_UserProfile).GetProperty(y)!.GetValue(x.Key)!.ToString()), x.Value))
                  .OrderByDescending(x => x.Value);
    }

    /// <inheritdoc />
    public async Task<QueryUserProfileRsp> CreateAsync(CreateUserProfileReq req)
    {
        req.ThrowIfInvalid();
        var entity = req.Adapt<Sys_UserProfile>();
        var ret    = await Rpo.InsertAsync(entity).ConfigureAwait(false);
        return ret.Adapt<QueryUserProfileRsp>();
    }

    /// <inheritdoc />
    public Task<int> DeleteAsync(DelReq req)
    {
        req.ThrowIfInvalid();
        return Rpo.DeleteAsync(a => a.Id == req.Id);
    }

    /// <inheritdoc />
    public async Task<QueryUserProfileRsp> EditAsync(EditUserProfileReq req)
    {
        req.ThrowIfInvalid();

        return
            #if DBTYPE_SQLSERVER
            (await UpdateReturnListAsync(req.Adapt<Sys_UserProfile>()).ConfigureAwait(false)).FirstOrDefault()?.Adapt<QueryUserProfileRsp>();
            #else
            await UpdateAsync(req.Adapt<Sys_UserProfile>()).ConfigureAwait(false) > 0
                ? await GetAsync(new QueryUserProfileReq { Id = req.Id }).ConfigureAwait(false)
                : null;
        #endif
    }

    /// <inheritdoc />
    public Task<IActionResult> ExportAsync(QueryReq<QueryUserProfileReq> req)
    {
        req.ThrowIfInvalid();
        throw new NotImplementedException();
    }

    /// <inheritdoc />
    public async Task<QueryUserProfileRsp> GetAsync(QueryUserProfileReq req)
    {
        req.ThrowIfInvalid();
        var ret = await QueryInternalComplex(new QueryReq<QueryUserProfileReq> { Filter = req, Order = Orders.None })
                        .ToOneAsync()
                        .ConfigureAwait(false);
        return ret.Adapt<QueryUserProfileRsp>();
    }

    /// <inheritdoc />
    public async Task<GetSessionUserAppConfigRsp> GetSessionUserAppConfigAsync()
    {
        var ret = await Rpo.Select.Where(a => a.Id == UserToken.Id).ToOneAsync().ConfigureAwait(false);
        return ret.Adapt<GetSessionUserAppConfigRsp>();
    }

    /// <inheritdoc />
    public async Task<PagedQueryRsp<QueryUserProfileRsp>> PagedQueryAsync(PagedQueryReq<QueryUserProfileReq> req)
    {
        req.ThrowIfInvalid();
        var list = await QueryInternalComplex(req)
                         .Page(req.Page, req.PageSize)
                         .WithNoLockNoWait()
                         .Count(out var total)
                         .ToListAsync((a, b, c, d, e) => new {
                                                                 a
                                                               , b = new { b.Key, b.Value }
                                                               , c = new { c.Key, c.Value }
                                                               , d = new { d.Key, d.Value }
                                                               , e = new { e.Key, e.Value }
                                                             })
                         .ConfigureAwait(false);

        return new PagedQueryRsp<QueryUserProfileRsp>(req.Page, req.PageSize, total
                                                    , list.ConvertAll(x => x.a.Adapt<QueryUserProfileRsp>() with {
                                                                               NationArea = x.b.Adapt<QueryDicContentRsp>()
                                                                             , CompanyArea = x.c.Adapt<QueryDicContentRsp>()
                                                                             , HomeArea = x.d.Adapt<QueryDicContentRsp>()
                                                                             , EmergencyContactArea = x.e.Adapt<QueryDicContentRsp>()
                                                                           }));
    }

    /// <inheritdoc />
    public async Task<IEnumerable<QueryUserProfileRsp>> QueryAsync(QueryReq<QueryUserProfileReq> req)
    {
        req.ThrowIfInvalid();
        var ret = await QueryInternalComplex(req)
                        .WithNoLockNoWait()
                        .Take(req.Count)
                        .ToListAsync((a, b, c, d, e) => new {
                                                                a
                                                              , b = new { b.Key, b.Value }
                                                              , c = new { c.Key, c.Value }
                                                              , d = new { d.Key, d.Value }
                                                              , e = new { e.Key, e.Value }
                                                            })
                        .ConfigureAwait(false);
        return ret.ConvertAll(x => x.a.Adapt<QueryUserProfileRsp>() with {
                                                                             NationArea = x.b.Key  == null ? null : x.b.Adapt<QueryDicContentRsp>()
                                                                           , CompanyArea = x.c.Key == null ? null : x.c.Adapt<QueryDicContentRsp>()
                                                                           , HomeArea = x.d.Key    == null ? null : x.d.Adapt<QueryDicContentRsp>()
                                                                           , EmergencyContactArea
                                                                             = x.e.Key == null ? null : x.e.Adapt<QueryDicContentRsp>()
                                                                         });
    }

    /// <inheritdoc />
    public Task<int> SetSessionUserAppConfigAsync(SetSessionUserAppConfigReq req)
    {
        req.ThrowIfInvalid();

        // 默认仪表版
        if (req.AppConfig == "[]") {
            req.AppConfig = BuildAppConfig(S<ContextUserInfo>().Roles.ToDictionary(x => x.Id, x => x.DashboardLayout));
        }

        return UpdateAsync(req, [nameof(req.AppConfig)], null, a => a.Id == UserToken.Id, null, true);
    }

    private ISelect<Sys_UserProfile> QueryInternal(QueryReq<QueryUserProfileReq> req)
    {
        var ret = Rpo.Select.WhereDynamicFilter(req.DynamicFilter).WhereDynamic(req.Filter);

        // ReSharper disable once SwitchStatementMissingSomeEnumCasesNoDefault
        switch (req.Order) {
            case Orders.None:
                return ret;
            case Orders.Random:
                return ret.OrderByRandom();
        }

        ret = ret.OrderByPropertyNameIf(req.Prop?.Length > 0, req.Prop, req.Order == Orders.Ascending);
        if (!req.Prop?.Equals(nameof(req.Filter.Id), StringComparison.OrdinalIgnoreCase) ?? true) {
            ret = ret.OrderByDescending(a => a.Id);
        }

        return ret;
    }

    private ISelect<Sys_UserProfile, Sys_DicContent, Sys_DicContent, Sys_DicContent, Sys_DicContent> QueryInternalComplex(
        QueryReq<QueryUserProfileReq> req)
    {
        #pragma warning disable CA1305
        var ret = Rpo.Orm.Select<Sys_UserProfile, Sys_DicContent, Sys_DicContent, Sys_DicContent, Sys_DicContent>()
                     .LeftJoin((a, b, _,  __,  ___) => a.NationArea.ToString()         == b.Value && b.CatalogId == Numbers.ID_DIC_CATALOG_GEO_AREA)
                     .LeftJoin((a, _, c,  __,  ___) => a.CompanyArea.ToString()        == c.Value && c.CatalogId == Numbers.ID_DIC_CATALOG_GEO_AREA)
                     .LeftJoin((a, _, __, d,   ___) => a.HomeArea.ToString()           == d.Value && d.CatalogId == Numbers.ID_DIC_CATALOG_GEO_AREA)
                     .LeftJoin((a, _, __, ___, e) => a.EmergencyContactArea.ToString() == e.Value && e.CatalogId == Numbers.ID_DIC_CATALOG_GEO_AREA)
                     .WhereDynamicFilter(req.DynamicFilter);

        return req.Order switch {
                   Orders.None   => ret
                 , Orders.Random => ret.OrderByRandom()
                 , _ => ret.OrderByPropertyNameIf(req.Prop?.Length > 0, req.Prop, req.Order == Orders.Ascending)
                           .OrderByDescending((a, _, __, ___, ____) => a.Id)
               };
        #pragma warning restore CA1305
    }
}