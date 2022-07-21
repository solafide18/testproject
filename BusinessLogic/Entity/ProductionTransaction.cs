using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using BusinessLogic.Utilities;
using DataAccess;
using DataAccess.Repository;
using NLog;
using Omu.ValueInjecter;
using PetaPoco;
using PetaPoco.Providers;

namespace BusinessLogic.Entity
{
    public partial class ProductionTransaction: ServiceRepository<production_transaction, vw_production_transaction>
    {
        private static readonly Logger logger = LogManager.GetCurrentClassLogger();

        private readonly UserContext userContext;

        public ProductionTransaction(UserContext userContext)
            : base(userContext.GetDataContext())
        {
            this.userContext = userContext;
        }

        public static async Task<StandardResult> UpdateStockState(string ConnectionString, production_transaction TransactionRecord)
        {
            var result = new StandardResult();
            logger.Trace($"UpdateStockState; TransactionRecord.id = {0}", TransactionRecord.id);

            try
            {
                var db = new Database(ConnectionString, new PostgreSQLDatabaseProvider());
                using (var tx = db.GetTransaction())
                {
                    try
                    {
                        if (TransactionRecord != null && TransactionRecord.unloading_datetime != null)
                        {
                            var record = await db.FirstOrDefaultAsync<production_transaction>(
                                "WHERE id = @0", TransactionRecord.id);

                            if(record != null)
                            {
                                #region Latest quality sampling

                                var qualitySampling = await db.FirstOrDefaultAsync<quality_sampling>(
                                    " WHERE stock_location_id = @0 "
                                    + " AND sampling_datetime <= @1 "
                                    + " ORDER BY sampling_datetime DESC ",
                                    TransactionRecord.source_location_id,
                                    TransactionRecord.unloading_datetime);

                                logger.Debug(db.LastCommand);
                                #endregion

                                #region Source stock state

                                var sourceStockState = await db.FirstOrDefaultAsync<stock_state>(
                                    "WHERE transaction_id = @0 AND stock_location_id = @1",
                                    TransactionRecord.id, TransactionRecord.source_location_id);
                                if (sourceStockState == null)
                                {
                                    sourceStockState = new stock_state()
                                    {
                                        created_by = record.modified_by ?? record.created_by,
                                        created_on = DateTime.Now,
                                        is_active = true,
                                        owner_id = record.owner_id,
                                        organization_id = record.organization_id,
                                        transaction_id = TransactionRecord.id,
                                        transaction_datetime = TransactionRecord.unloading_datetime,
                                        stock_location_id = TransactionRecord.source_location_id,
                                        product_out_id = TransactionRecord.product_id,
                                        quality_sampling_id = TransactionRecord.quality_sampling_id ?? qualitySampling?.id
                                    };
                                }
                                else
                                {
                                    sourceStockState.transaction_datetime = TransactionRecord.unloading_datetime;
                                    sourceStockState.quality_sampling_id = TransactionRecord.quality_sampling_id ?? qualitySampling?.id;
                                }
                                logger.Debug(db.LastCommand);

                                // Get previous source stock state
                                var prevSourceStockState = await db.FirstOrDefaultAsync<stock_state>(
                                    "WHERE stock_location_id = @0 AND transaction_datetime < @1 ORDER BY transaction_datetime DESC",
                                    TransactionRecord.source_location_id, TransactionRecord.unloading_datetime);

                                sourceStockState.qty_opening = prevSourceStockState?.qty_closing ?? 0;
                                sourceStockState.qty_in = 0;
                                sourceStockState.qty_out = record.unloading_quantity;
                                sourceStockState.qty_closing = (sourceStockState.qty_opening ?? 0)
                                    - (sourceStockState.qty_out ?? 0);

                                if (string.IsNullOrEmpty(sourceStockState.id))
                                {
                                    sourceStockState.id = Guid.NewGuid().ToString("N").ToLower();
                                    await db.InsertAsync(sourceStockState);
                                }
                                else
                                {
                                    sourceStockState.modified_by = record.modified_by ?? record.created_by;
                                    sourceStockState.modified_on = DateTime.Now;
                                    await db.UpdateAsync(sourceStockState);
                                }
                                logger.Debug(db.LastCommand);

                                // Modify all subsequent stock state
                                var nextSourceStockStates = await db.FetchAsync<stock_state>(
                                    "WHERE stock_location_id = @0 AND transaction_datetime > @1",
                                    TransactionRecord.source_location_id, TransactionRecord.unloading_datetime);
                                if (nextSourceStockStates != null && nextSourceStockStates.Count > 0)
                                {
                                    var qty_opening = sourceStockState.qty_closing ?? 0;
                                    foreach (var nextSourceStockState in nextSourceStockStates.OrderBy(o => o.transaction_datetime))
                                    {
                                        nextSourceStockState.qty_opening = qty_opening;
                                        nextSourceStockState.qty_closing = nextSourceStockState.qty_opening +
                                            (nextSourceStockState.qty_in ?? 0) - (nextSourceStockState.qty_out ?? 0);
                                        nextSourceStockState.modified_by = record.modified_by ?? record.created_by;
                                        nextSourceStockState.modified_on = DateTime.Now;

                                        if (nextSourceStockState.qty_survey != null)
                                        {
                                            nextSourceStockState.qty_closing = nextSourceStockState.qty_survey;
                                            await db.UpdateAsync(nextSourceStockState);
                                            break;
                                        }
                                        else
                                        {
                                            await db.UpdateAsync(nextSourceStockState);
                                            qty_opening = nextSourceStockState.qty_closing ?? 0;
                                        }
                                    }
                                }
                                logger.Debug(db.LastCommand);

                                #endregion

                                #region Destination stock state

                                var destinationStockState = await db.FirstOrDefaultAsync<stock_state>(
                                    "WHERE transaction_id = @0 AND stock_location_id = @1",
                                    TransactionRecord.id, TransactionRecord.destination_location_id);
                                if (destinationStockState == null)
                                {
                                    destinationStockState = new stock_state()
                                    {
                                        created_by = record.modified_by ?? record.created_by,
                                        created_on = DateTime.Now,
                                        is_active = true,
                                        owner_id = record.owner_id,
                                        organization_id = record.organization_id,
                                        transaction_id = TransactionRecord.id,
                                        transaction_datetime = TransactionRecord.unloading_datetime,
                                        stock_location_id = TransactionRecord.destination_location_id,
                                        product_in_id = TransactionRecord.product_id,
                                        quality_sampling_id = TransactionRecord.quality_sampling_id ?? qualitySampling?.id
                                    };
                                }
                                else
                                {
                                    destinationStockState.transaction_datetime = TransactionRecord.unloading_datetime;
                                    destinationStockState.quality_sampling_id = TransactionRecord.quality_sampling_id ?? qualitySampling?.id;
                                }
                                logger.Debug(db.LastCommand);

                                // Get previous destination stock state
                                var prevDestinationStockState = await db.FirstOrDefaultAsync<stock_state>(
                                    "WHERE stock_location_id = @0 AND transaction_datetime < @1 ORDER BY transaction_datetime DESC",
                                    TransactionRecord.destination_location_id, TransactionRecord.unloading_datetime);

                                destinationStockState.qty_opening = prevDestinationStockState?.qty_closing ?? 0;
                                destinationStockState.qty_in = record.unloading_quantity;
                                destinationStockState.qty_out = 0;
                                destinationStockState.qty_closing = (destinationStockState.qty_opening ?? 0)
                                    + (destinationStockState.qty_in ?? 0);

                                if (string.IsNullOrEmpty(destinationStockState.id))
                                {
                                    destinationStockState.id = Guid.NewGuid().ToString("N").ToLower();
                                    await db.InsertAsync(destinationStockState);
                                }
                                else
                                {
                                    destinationStockState.modified_by = record.modified_by ?? record.created_by;
                                    destinationStockState.modified_on = DateTime.Now;
                                    await db.UpdateAsync(destinationStockState);
                                }
                                logger.Debug(db.LastCommand);

                                // Modify all subsequent stock state
                                var nextDestinationStockStates = await db.FetchAsync<stock_state>(
                                    "WHERE stock_location_id = @0 AND transaction_datetime > @1",
                                    TransactionRecord.destination_location_id, TransactionRecord.unloading_datetime);
                                if (nextDestinationStockStates != null && nextDestinationStockStates.Count > 0)
                                {
                                    var qty_opening = destinationStockState.qty_closing ?? 0;
                                    foreach (var nextDestinationStockState in nextDestinationStockStates.OrderBy(o => o.transaction_datetime))
                                    {
                                        nextDestinationStockState.qty_opening = qty_opening;
                                        nextDestinationStockState.qty_closing = nextDestinationStockState.qty_opening +
                                            (nextDestinationStockState.qty_in ?? 0) - (nextDestinationStockState.qty_out ?? 0);
                                        nextDestinationStockState.modified_by = record.modified_by ?? record.created_by;
                                        nextDestinationStockState.modified_on = DateTime.Now;

                                        if (nextDestinationStockState.qty_survey != null)
                                        {
                                            nextDestinationStockState.qty_closing = nextDestinationStockState.qty_survey;
                                            await db.UpdateAsync(nextDestinationStockState);
                                            break;
                                        }
                                        else
                                        {
                                            await db.UpdateAsync(nextDestinationStockState);
                                            qty_opening = nextDestinationStockState.qty_closing ?? 0;
                                        }
                                    }
                                }
                                logger.Debug(db.LastCommand);

                                #endregion
                            }
                            else // Handle deleted transaction
                            {
                                #region Source stock state

                                var sourceStockState = await db.FirstOrDefaultAsync<stock_state>(
                                    "WHERE transaction_id = @0 AND stock_location_id = @1",
                                    TransactionRecord.id, TransactionRecord.source_location_id);
                                if (sourceStockState != null)
                                {
                                    sourceStockState.transaction_datetime = TransactionRecord.unloading_datetime;

                                    // Get previous source stock state
                                    var prevSourceStockState = await db.FirstOrDefaultAsync<stock_state>(
                                        "WHERE stock_location_id = @0 AND transaction_datetime < @1 ORDER BY transaction_datetime DESC",
                                        TransactionRecord.source_location_id, TransactionRecord.unloading_datetime);

                                    sourceStockState.qty_opening = prevSourceStockState?.qty_closing ?? 0;
                                    sourceStockState.qty_in = 0;
                                    sourceStockState.qty_out = 0;
                                    sourceStockState.qty_closing = sourceStockState.qty_opening;
                                    sourceStockState.modified_by = record.modified_by ?? record.created_by;
                                    sourceStockState.modified_on = DateTime.Now;
                                    await db.UpdateAsync(sourceStockState);

                                    logger.Debug(db.LastCommand);

                                    // Modify all subsequent stock state
                                    var nextSourceStockStates = await db.FetchAsync<stock_state>(
                                        "WHERE stock_location_id = @0 AND transaction_datetime > @1",
                                        TransactionRecord.source_location_id, TransactionRecord.unloading_datetime);
                                    if (nextSourceStockStates != null && nextSourceStockStates.Count > 0)
                                    {
                                        var qty_opening = sourceStockState.qty_closing ?? 0;
                                        foreach (var nextSourceStockState in nextSourceStockStates.OrderBy(o => o.transaction_datetime))
                                        {
                                            nextSourceStockState.qty_opening = qty_opening;
                                            nextSourceStockState.qty_closing = nextSourceStockState.qty_opening +
                                                (nextSourceStockState.qty_in ?? 0) - (nextSourceStockState.qty_out ?? 0);
                                            nextSourceStockState.modified_by = record.modified_by ?? record.created_by;
                                            nextSourceStockState.modified_on = DateTime.Now;

                                            if (nextSourceStockState.qty_survey != null)
                                            {
                                                nextSourceStockState.qty_closing = nextSourceStockState.qty_survey;
                                                await db.UpdateAsync(nextSourceStockState);
                                                break;
                                            }
                                            else
                                            {
                                                await db.UpdateAsync(nextSourceStockState);
                                                qty_opening = nextSourceStockState.qty_closing ?? 0;
                                            }
                                        }
                                    }
                                }
                                logger.Debug(db.LastCommand);

                                #endregion

                                #region Destination stock state

                                var destinationStockState = await db.FirstOrDefaultAsync<stock_state>(
                                    "WHERE transaction_id = @0 AND stock_location_id = @1",
                                    TransactionRecord.id, TransactionRecord.destination_location_id);
                                if (destinationStockState != null)
                                {
                                    destinationStockState.transaction_datetime = TransactionRecord.unloading_datetime;

                                    // Get previous destination stock state
                                    var prevDestinationStockState = await db.FirstOrDefaultAsync<stock_state>(
                                        "WHERE stock_location_id = @0 AND transaction_datetime < @1 ORDER BY transaction_datetime DESC",
                                        TransactionRecord.destination_location_id, TransactionRecord.unloading_datetime);

                                    destinationStockState.qty_opening = prevDestinationStockState?.qty_closing ?? 0;
                                    destinationStockState.qty_in = 0;
                                    destinationStockState.qty_out = 0;
                                    destinationStockState.qty_closing = destinationStockState.qty_opening;
                                    destinationStockState.modified_by = record.modified_by ?? record.created_by;
                                    destinationStockState.modified_on = DateTime.Now;
                                    await db.UpdateAsync(destinationStockState);
                                    logger.Debug(db.LastCommand);

                                    // Modify all subsequent stock state
                                    var nextDestinationStockStates = await db.FetchAsync<stock_state>(
                                        "WHERE stock_location_id = @0 AND transaction_datetime > @1",
                                        TransactionRecord.destination_location_id, TransactionRecord.unloading_datetime);
                                    if (nextDestinationStockStates != null && nextDestinationStockStates.Count > 0)
                                    {
                                        var qty_opening = destinationStockState.qty_closing ?? 0;
                                        foreach (var nextDestinationStockState in nextDestinationStockStates.OrderBy(o => o.transaction_datetime))
                                        {
                                            nextDestinationStockState.qty_opening = qty_opening;
                                            nextDestinationStockState.qty_closing = nextDestinationStockState.qty_opening +
                                                (nextDestinationStockState.qty_in ?? 0) - (nextDestinationStockState.qty_out ?? 0);
                                            nextDestinationStockState.modified_by = record.modified_by ?? record.created_by;
                                            nextDestinationStockState.modified_on = DateTime.Now;

                                            if (nextDestinationStockState.qty_survey != null)
                                            {
                                                nextDestinationStockState.qty_closing = nextDestinationStockState.qty_survey;
                                                await db.UpdateAsync(nextDestinationStockState);
                                                break;
                                            }
                                            else
                                            {
                                                await db.UpdateAsync(nextDestinationStockState);
                                                qty_opening = nextDestinationStockState.qty_closing ?? 0;
                                            }
                                        }
                                    }
                                    logger.Debug(db.LastCommand);
                                }

                                #endregion
                            }

                            tx.Complete();
                            result.Success = true;
                        }
                    }
                    catch (Exception ex)
                    {
                        logger.Debug(db.LastCommand);
                        logger.Error(ex);
                        result.Message = ex.InnerException?.Message ?? ex.Message;
                    }
                }
            }
            catch (Exception ex)
            {
                logger.Error(ex);
                result.Message = ex.InnerException?.Message ?? ex.Message;
            }

            return result;
        }

        public static async Task<StandardResult> UpdateStockStateAnalyte(string ConnectionString,
            string QualitySamplingId, production_transaction TransactionRecord)
        {
            var result = new StandardResult();

            try
            {
                var db = new Database(ConnectionString, new PostgreSQLDatabaseProvider());
                using (var tx = db.GetTransaction())
                {
                    try
                    {
                        if (TransactionRecord != null)
                        {
                            var record = await db.FirstOrDefaultAsync<production_transaction>(
                                "WHERE id = @0", TransactionRecord.id);

                            if (record != null)
                            {
                                #region Latest quality sampling

                                var qualitySampling = await db.FirstOrDefaultAsync<quality_sampling>(
                                    " WHERE stock_location_id = @0 "
                                    + " AND sampling_datetime <= @1 "
                                    + " ORDER BY sampling_datetime DESC ",
                                    TransactionRecord.source_location_id,
                                    TransactionRecord.unloading_datetime);

                                #endregion

                                #region Source stock state

                                var sourceStockState = await db.FirstOrDefaultAsync<stock_state>(
                                    "WHERE transaction_id = @0 AND stock_location_id = @1",
                                    TransactionRecord.id, TransactionRecord.source_location_id);
                                if (sourceStockState == null)
                                {
                                    sourceStockState = new stock_state()
                                    {
                                        created_by = record.modified_by ?? record.created_by,
                                        created_on = DateTime.Now,
                                        is_active = true,
                                        owner_id = record.owner_id,
                                        organization_id = record.organization_id,
                                        transaction_id = TransactionRecord.id,
                                        transaction_datetime = TransactionRecord.unloading_datetime,
                                        stock_location_id = TransactionRecord.source_location_id,
                                        product_out_id = TransactionRecord.product_id,
                                        quality_sampling_id = TransactionRecord.quality_sampling_id ?? qualitySampling?.id
                                    };
                                }
                                else
                                {
                                    sourceStockState.transaction_datetime = TransactionRecord.unloading_datetime;
                                    sourceStockState.quality_sampling_id = TransactionRecord.quality_sampling_id ?? qualitySampling?.id;
                                }

                                // Get previous source stock state
                                var prevSourceStockState = await db.FirstOrDefaultAsync<stock_state>(
                                    "WHERE stock_location_id = @0 AND transaction_datetime < @1 ORDER BY transaction_datetime DESC",
                                    TransactionRecord.source_location_id, TransactionRecord.loading_datetime);

                                sourceStockState.qty_opening = prevSourceStockState?.qty_closing ?? 0;
                                sourceStockState.qty_in = 0;
                                sourceStockState.qty_out = record.unloading_quantity;
                                sourceStockState.qty_closing = (sourceStockState.qty_opening ?? 0)
                                    - (sourceStockState.qty_out ?? 0);

                                if (string.IsNullOrEmpty(sourceStockState.id))
                                {
                                    sourceStockState.id = Guid.NewGuid().ToString("N").ToLower();
                                    await db.InsertAsync(sourceStockState);
                                }
                                else
                                {
                                    sourceStockState.modified_by = record.modified_by ?? record.created_by;
                                    sourceStockState.modified_on = DateTime.Now;
                                    await db.UpdateAsync(sourceStockState);
                                }

                                // Modify all subsequent stock state
                                var nextSourceStockStates = await db.FetchAsync<stock_state>(
                                    "WHERE stock_location_id = @0 AND transaction_datetime > @1",
                                    TransactionRecord.source_location_id, TransactionRecord.unloading_datetime);
                                if (nextSourceStockStates != null && nextSourceStockStates.Count > 0)
                                {
                                    var qty_opening = sourceStockState.qty_closing ?? 0;
                                    foreach (var nextSourceStockState in nextSourceStockStates.OrderBy(o => o.transaction_datetime))
                                    {
                                        nextSourceStockState.qty_opening = qty_opening;
                                        nextSourceStockState.qty_closing = nextSourceStockState.qty_opening +
                                            (nextSourceStockState.qty_in ?? 0) - (nextSourceStockState.qty_out ?? 0);
                                        nextSourceStockState.modified_by = record.modified_by ?? record.created_by;
                                        nextSourceStockState.modified_on = DateTime.Now;

                                        if (nextSourceStockState.qty_survey != null)
                                        {
                                            nextSourceStockState.qty_closing = nextSourceStockState.qty_survey;
                                            await db.UpdateAsync(nextSourceStockState);
                                            break;
                                        }
                                        else
                                        {
                                            await db.UpdateAsync(nextSourceStockState);
                                            qty_opening = nextSourceStockState.qty_closing ?? 0;
                                        }
                                    }
                                }

                                #endregion

                                #region Destination stock state

                                var destinationStockState = await db.FirstOrDefaultAsync<stock_state>(
                                    "WHERE transaction_id = @0 AND stock_location_id = @1",
                                    TransactionRecord.id, TransactionRecord.destination_location_id);
                                if (destinationStockState == null)
                                {
                                    destinationStockState = new stock_state()
                                    {
                                        created_by = record.modified_by ?? record.created_by,
                                        created_on = DateTime.Now,
                                        is_active = true,
                                        owner_id = record.owner_id,
                                        organization_id = record.organization_id,
                                        transaction_id = TransactionRecord.id,
                                        transaction_datetime = TransactionRecord.unloading_datetime,
                                        stock_location_id = TransactionRecord.destination_location_id,
                                        product_in_id = TransactionRecord.product_id,
                                        quality_sampling_id = TransactionRecord.quality_sampling_id ?? qualitySampling?.id
                                    };
                                }
                                else
                                {
                                    destinationStockState.transaction_datetime = TransactionRecord.unloading_datetime;
                                    destinationStockState.quality_sampling_id = TransactionRecord.quality_sampling_id ?? qualitySampling?.id;
                                }

                                // Get previous destination stock state
                                var prevDestinationStockState = await db.FirstOrDefaultAsync<stock_state>(
                                    "WHERE stock_location_id = @0 AND transaction_datetime < @1 ORDER BY transaction_datetime DESC",
                                    TransactionRecord.destination_location_id, TransactionRecord.unloading_datetime);

                                destinationStockState.qty_opening = prevDestinationStockState?.qty_closing ?? 0;
                                destinationStockState.qty_in = record?.unloading_quantity ?? 0;
                                destinationStockState.qty_out = 0;
                                destinationStockState.qty_closing = (destinationStockState.qty_opening ?? 0)
                                    + (destinationStockState.qty_in ?? 0);

                                if (string.IsNullOrEmpty(destinationStockState.id))
                                {
                                    destinationStockState.id = Guid.NewGuid().ToString("N").ToLower();
                                    await db.InsertAsync(destinationStockState);
                                }
                                else
                                {
                                    destinationStockState.modified_by = record.modified_by ?? record.created_by;
                                    destinationStockState.modified_on = DateTime.Now;
                                    await db.UpdateAsync(destinationStockState);
                                }

                                // Modify all subsequent stock state
                                var nextDestinationStockStates = await db.FetchAsync<stock_state>(
                                    "WHERE stock_location_id = @0 AND transaction_datetime > @1",
                                    TransactionRecord.destination_location_id, TransactionRecord.unloading_datetime);
                                if (nextDestinationStockStates != null && nextDestinationStockStates.Count > 0)
                                {
                                    var qty_opening = destinationStockState.qty_closing ?? 0;
                                    foreach (var nextDestinationStockState in nextDestinationStockStates.OrderBy(o => o.transaction_datetime))
                                    {
                                        nextDestinationStockState.qty_opening = qty_opening;
                                        nextDestinationStockState.qty_closing = nextDestinationStockState.qty_opening +
                                            (nextDestinationStockState.qty_in ?? 0) - (nextDestinationStockState.qty_out ?? 0);
                                        nextDestinationStockState.modified_by = record.modified_by ?? record.created_by;
                                        nextDestinationStockState.modified_on = DateTime.Now;

                                        if (nextDestinationStockState.qty_survey != null)
                                        {
                                            nextDestinationStockState.qty_closing = nextDestinationStockState.qty_survey;
                                            await db.UpdateAsync(nextDestinationStockState);
                                            break;
                                        }
                                        else
                                        {
                                            await db.UpdateAsync(nextDestinationStockState);
                                            qty_opening = nextDestinationStockState.qty_closing ?? 0;
                                        }
                                    }
                                }

                                #endregion
                            }
                            else // Handle deleted transaction
                            {
                                #region Source stock state

                                var sourceStockState = await db.FirstOrDefaultAsync<stock_state>(
                                    "WHERE transaction_id = @0 AND stock_location_id = @1",
                                    TransactionRecord.id, TransactionRecord.source_location_id);
                                if (sourceStockState != null)
                                {
                                    sourceStockState.transaction_datetime = TransactionRecord.unloading_datetime;

                                    // Get previous source stock state
                                    var prevSourceStockState = await db.FirstOrDefaultAsync<stock_state>(
                                        "WHERE stock_location_id = @0 AND transaction_datetime < @1 ORDER BY transaction_datetime DESC",
                                        TransactionRecord.source_location_id, TransactionRecord.unloading_datetime);

                                    sourceStockState.qty_opening = prevSourceStockState?.qty_closing ?? 0;
                                    sourceStockState.qty_in = 0;
                                    sourceStockState.qty_out = 0;
                                    sourceStockState.qty_closing = sourceStockState.qty_opening;
                                    sourceStockState.modified_by = record.modified_by ?? record.created_by;
                                    sourceStockState.modified_on = DateTime.Now;
                                    await db.UpdateAsync(sourceStockState);

                                    // Modify all subsequent stock state
                                    var nextSourceStockStates = await db.FetchAsync<stock_state>(
                                        "WHERE stock_location_id = @0 AND transaction_datetime > @1",
                                        TransactionRecord.source_location_id, TransactionRecord.loading_datetime);
                                    if (nextSourceStockStates != null && nextSourceStockStates.Count > 0)
                                    {
                                        var qty_opening = sourceStockState.qty_closing ?? 0;
                                        foreach (var nextSourceStockState in nextSourceStockStates.OrderBy(o => o.transaction_datetime))
                                        {
                                            nextSourceStockState.qty_opening = qty_opening;
                                            nextSourceStockState.qty_closing = nextSourceStockState.qty_opening +
                                                (nextSourceStockState.qty_in ?? 0) - (nextSourceStockState.qty_out ?? 0);
                                            nextSourceStockState.modified_by = record.modified_by ?? record.created_by;
                                            nextSourceStockState.modified_on = DateTime.Now;

                                            if (nextSourceStockState.qty_survey != null)
                                            {
                                                nextSourceStockState.qty_closing = nextSourceStockState.qty_survey;
                                                await db.UpdateAsync(nextSourceStockState);
                                                break;
                                            }
                                            else
                                            {
                                                await db.UpdateAsync(nextSourceStockState);
                                                qty_opening = nextSourceStockState.qty_closing ?? 0;
                                            }
                                        }
                                    }
                                }

                                #endregion

                                #region Destination stock state

                                var destinationStockState = await db.FirstOrDefaultAsync<stock_state>(
                                    "WHERE transaction_id = @0 AND stock_location_id = @1",
                                    TransactionRecord.id, TransactionRecord.destination_location_id);
                                if (destinationStockState != null)
                                {
                                    destinationStockState.transaction_datetime = TransactionRecord.unloading_datetime;

                                    // Get previous destination stock state
                                    var prevDestinationStockState = await db.FirstOrDefaultAsync<stock_state>(
                                        "WHERE stock_location_id = @0 AND transaction_datetime < @1 ORDER BY transaction_datetime DESC",
                                        TransactionRecord.destination_location_id, TransactionRecord.unloading_datetime);

                                    destinationStockState.qty_opening = prevDestinationStockState?.qty_closing ?? 0;
                                    destinationStockState.qty_in = 0;
                                    destinationStockState.qty_out = 0;
                                    destinationStockState.qty_closing = destinationStockState.qty_opening;
                                    destinationStockState.modified_by = record.modified_by ?? record.created_by;
                                    destinationStockState.modified_on = DateTime.Now;
                                    await db.UpdateAsync(destinationStockState);

                                    // Modify all subsequent stock state
                                    var nextDestinationStockStates = await db.FetchAsync<stock_state>(
                                        "WHERE stock_location_id = @0 AND transaction_datetime > @1",
                                        TransactionRecord.destination_location_id, TransactionRecord.unloading_datetime);
                                    if (nextDestinationStockStates != null && nextDestinationStockStates.Count > 0)
                                    {
                                        var qty_opening = destinationStockState.qty_closing ?? 0;
                                        foreach (var nextDestinationStockState in nextDestinationStockStates.OrderBy(o => o.transaction_datetime))
                                        {
                                            nextDestinationStockState.qty_opening = qty_opening;
                                            nextDestinationStockState.qty_closing = nextDestinationStockState.qty_opening +
                                                (nextDestinationStockState.qty_in ?? 0) - (nextDestinationStockState.qty_out ?? 0);
                                            nextDestinationStockState.modified_by = record.modified_by ?? record.created_by;
                                            nextDestinationStockState.modified_on = DateTime.Now;

                                            if (nextDestinationStockState.qty_survey != null)
                                            {
                                                nextDestinationStockState.qty_closing = nextDestinationStockState.qty_survey;
                                                await db.UpdateAsync(nextDestinationStockState);
                                                break;
                                            }
                                            else
                                            {
                                                await db.UpdateAsync(nextDestinationStockState);
                                                qty_opening = nextDestinationStockState.qty_closing ?? 0;
                                            }
                                        }
                                    }
                                }

                                #endregion
                            }
                        }
                        else
                        {
                            result.Message = "TransactionRecord is null.";
                            logger.Debug(result.Message);
                        }
                    }
                    catch (Exception ex)
                    {
                        logger.Debug(db.LastCommand);
                        logger.Error(ex);
                        result.Message = ex.InnerException?.Message ?? ex.Message;
                    }
                }
            }
            catch (Exception ex)
            {
                logger.Error(ex);
                result.Message = ex.InnerException?.Message ?? ex.Message;
            }

            return result;
        }
    }
}
