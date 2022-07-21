using System;
using System.Linq;
using System.Collections.Generic;
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
    public partial class QualitySampling: ServiceRepository<quality_sampling, vw_quality_sampling>
    {
        private static readonly Logger logger = LogManager.GetCurrentClassLogger();

        private readonly UserContext userContext;

        public QualitySampling(UserContext userContext)
            : base(userContext.GetDataContext())
        {
            this.userContext = userContext;
        }

        public static async Task<StandardResult> UpdateStockState(string ConnectionString, quality_sampling TransactionRecord)
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
                        if (TransactionRecord != null && TransactionRecord.approved_by != null)
                        {
                            var record = await db.FirstOrDefaultAsync<quality_sampling>(
                                "WHERE id = @0", TransactionRecord.id);
                            if(record != null)
                            {
                                var stockStates = await db.FetchAsync<stock_state>(
                                    "WHERE quality_sampling_id = @0", record.id);

                                var qsAnalytes = await db.FetchAsync<quality_sampling_analyte>(
                                    "WHERE quality_sampling_id = @0", record.id);

                                if(stockStates != null && stockStates.Count > 0
                                    && qsAnalytes != null && qsAnalytes.Count > 0)
                                {
                                    foreach (var qsAnalyte in qsAnalytes)
                                    {
                                        foreach (var stockState in stockStates.OrderBy(o => o.transaction_datetime))
                                        {
                                            #region Insert / update stock_state_analyte

                                            var ssa = await db.FirstOrDefaultAsync<stock_state_analyte>(
                                                "WHERE stock_state_id = @0 AND analyte_id = @1",
                                                stockState.id, qsAnalyte.analyte_id);
                                            if (ssa == null)
                                            {
                                                var e = new entity();
                                                e.InjectFrom(stockState);

                                                ssa = new stock_state_analyte();
                                                ssa.InjectFrom(e);

                                                ssa.id = Guid.NewGuid().ToString("N").ToLower();
                                                ssa.entity_id = null;
                                                ssa.stock_state_id = stockState.id;
                                                ssa.analyte_id = qsAnalyte.analyte_id;
                                                ssa.uom_id = qsAnalyte.uom_id;
                                                ssa.analyte_value = qsAnalyte.analyte_value;
                                                ssa.weighted_value = ((stockState.qty_in ?? 0) - (stockState.qty_out ?? 0))
                                                    * qsAnalyte.analyte_value;
                                                await db.InsertAsync(ssa);
                                            }
                                            else
                                            {
                                                ssa.analyte_value = qsAnalyte.analyte_value;
                                                ssa.weighted_value = ((stockState.qty_in ?? 0) - (stockState.qty_out ?? 0))
                                                    * qsAnalyte.analyte_value;
                                                await db.UpdateAsync(ssa);
                                            }

                                            #endregion

                                            #region Calculate analyte moving-average-value

                                            var sql = Sql.Builder.Append("SELECT r.id,");
                                            sql.Append("AVG(r.weighted_value) OVER(ORDER BY ss.transaction_datetime ROWS BETWEEN 1000 PRECEDING AND CURRENT ROW) AS moving_avg_value");
                                            sql.Append("FROM stock_state_analyte r");
                                            sql.Append("INNER JOIN stock_state ss ON ss.id = r.stock_state_id");
                                            sql.Append("WHERE ss.stock_location_id = @0", stockState.stock_location_id);
                                            sql.Append("AND ss.transaction_datetime <= @0", stockState.transaction_datetime);
                                            sql.Append("AND ssa.analyte_id = @0", qsAnalyte.analyte_id);
                                            sql.Append("AND ssa.analyte_value IS NOT NULL");
                                            sql.Append("ORDER BY ss.transaction_datetime DESC");
                                            sql.Append("LIMIT 1");
                                            var ma = await db.FirstOrDefaultAsync<dynamic>(sql);
                                            if (ma != null && ma.id != null && ma.moving_avg_value != null)
                                            {
                                                sql = Sql.Builder.Append("UPDATE stock_state_analyte");
                                                sql.Append("SET moving_avg_value = @0", (decimal)ma.moving_avg_value);
                                                sql.Append("WHERE id = @0", (string)ma.id);
                                                await db.ExecuteAsync(sql);
                                            }
                                            logger.Debug(db.LastCommand);

                                            #endregion
                                        }
                                    }
                                }
                                logger.Debug(db.LastCommand);
                            }
                            else // Handle deleted record
                            {
                                var sql = Sql.Builder.Append("SELECT stock_location_id,");
                                sql.Append("MIN(transaction_datetime) AS transaction_datetime");
                                sql.Append("FROM stock_state");
                                sql.Append("WHERE quality_sampling_id = @0", TransactionRecord.id);
                                sql.Append("GROUP BY stock_location_id");
                                var stockLocations = await db.FetchAsync<dynamic>(sql);
                                logger.Debug(db.LastCommand);

                                sql = Sql.Builder.Append("DELETE FROM stock_state_analyte");
                                sql.Append("WHERE stock_state_id IN (");
                                sql.Append("SELECT id FROM stock_state");
                                sql.Append("WHERE quality_sampling_id = @0", TransactionRecord.id);
                                sql.Append(")");
                                await db.ExecuteAsync(sql);
                                logger.Debug(db.LastCommand);

                                var qsAnalytes = await db.FetchAsync<analyte>();
                                if(stockLocations != null && stockLocations.Count > 0
                                    && qsAnalytes != null && qsAnalytes.Count > 0)
                                {
                                    foreach (var qsAnalyte in qsAnalytes)
                                    {
                                        foreach (var stockLocation in stockLocations)
                                        {
                                            #region Calculate analyte moving-average-value

                                            sql = Sql.Builder.Append("SELECT r.id,");
                                            sql.Append("AVG(r.weighted_value) OVER(ORDER BY ss.transaction_datetime ROWS BETWEEN 1000 PRECEDING AND CURRENT ROW) AS moving_avg_value");
                                            sql.Append("FROM stock_state_analyte r");
                                            sql.Append("INNER JOIN stock_state ss ON ss.id = r.stock_state_id");
                                            sql.Append("WHERE ss.stock_location_id = @0", (string)stockLocation.stock_location_id);
                                            sql.Append("AND ss.transaction_datetime >= @0", (DateTime)stockLocation.transaction_datetime);
                                            sql.Append("AND ssa.analyte_id = @0", (string)qsAnalyte.id);
                                            sql.Append("AND ssa.analyte_value IS NOT NULL");
                                            sql.Append("ORDER BY ss.transaction_datetime");
                                            sql.Append("LIMIT 1");
                                            var ma = await db.FirstOrDefaultAsync<dynamic>(sql);
                                            if (ma != null && ma.id != null && ma.moving_avg_value != null)
                                            {
                                                sql = Sql.Builder.Append("UPDATE stock_state_analyte");
                                                sql.Append("SET moving_avg_value = @0", (decimal)ma.moving_avg_value);
                                                sql.Append("WHERE id = @0", (string)ma.id);
                                                await db.ExecuteAsync(sql);
                                            }

                                            #endregion
                                        }
                                    }
                                }
                                logger.Debug(db.LastCommand);

                                sql = Sql.Builder.Append("UPDATE stock_state");
                                sql.Append("SET quality_sampling_id = NULL");
                                sql.Append("WHERE quality_sampling_id = @0", TransactionRecord.id);
                                await db.ExecuteAsync(sql);
                                logger.Debug(db.LastCommand);
                            }

                            tx.Complete();
                            result.Success = true;
                        }
                    }
                    catch (Exception ex)
                    {
                        logger.Error(ex);
                        logger.Debug(db.LastCommand);
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

        public async Task<StandardResult> ApplyToTransactions(string TransactionCategory, string QualitySamplingId,
            List<string> TransactionIds)
        {
            var result = new StandardResult();

            #region Validation

            if(string.IsNullOrEmpty(TransactionCategory) || string.IsNullOrEmpty(QualitySamplingId)
                || TransactionIds?.Count < 0)
            {
                return result;
            }

            #endregion

            var dc = userContext.GetDataContext();
            var db = dc.Database;
            using (var tx = db.GetTransaction())
            {
                try
                {
                    var qs = await db.FirstOrDefaultAsync<quality_sampling>("WHERE id = @0", QualitySamplingId);
                    logger.Debug(db.LastCommand);

                    if (qs != null)
                    {
                        switch (TransactionCategory.ToLower())
                        {
                            case "hauling":
                                #region Hauling

                                foreach (var id in TransactionIds)
                                {
                                    var record = await db.FirstOrDefaultAsync<hauling_transaction>("WHERE id = @0", id);
                                    logger.Debug(db.LastCommand);
                                    if (record != null)
                                    {
                                        record.quality_sampling_id = qs.id;
                                        record.modified_by = userContext.AppUserId;
                                        record.modified_on = DateTime.Now;

                                        await db.UpdateAsync(record);
                                        logger.Debug(db.LastCommand);
                                    }
                                }

                                tx.Complete();
                                result.Success = true;

                                #endregion
                                break;

                            case "processing":
                                #region Processing

                                foreach (var id in TransactionIds)
                                {
                                    var record = await db.FirstOrDefaultAsync<processing_transaction>("WHERE id = @0", id);
                                    logger.Debug(db.LastCommand);
                                    if (record != null)
                                    {
                                        record.quality_sampling_id = qs.id;
                                        record.modified_by = userContext.AppUserId;
                                        record.modified_on = DateTime.Now;

                                        await db.UpdateAsync(record);
                                        logger.Debug(db.LastCommand);
                                    }
                                }

                                tx.Complete();
                                result.Success = true;

                                #endregion
                                break;

                            case "production":
                                #region Production

                                foreach (var id in TransactionIds)
                                {
                                    var record = await db.FirstOrDefaultAsync<production_transaction>("WHERE id = @0", id);
                                    logger.Debug(db.LastCommand);
                                    if (record != null)
                                    {
                                        record.quality_sampling_id = qs.id;
                                        record.modified_by = userContext.AppUserId;
                                        record.modified_on = DateTime.Now;

                                        await db.UpdateAsync(record);
                                        logger.Debug(db.LastCommand);
                                    }
                                }

                                tx.Complete();
                                result.Success = true;

                                #endregion
                                break;

                            case "rehandling":
                                #region Rehandling
                                foreach (var id in TransactionIds)
                                {
                                    var record = await db.FirstOrDefaultAsync<rehandling_transaction>("WHERE id = @0", id);
                                    logger.Debug(db.LastCommand);
                                    if (record != null)
                                    {
                                        record.quality_sampling_id = qs.id;
                                        record.modified_by = userContext.AppUserId;
                                        record.modified_on = DateTime.Now;

                                        await db.UpdateAsync(record);
                                        logger.Debug(db.LastCommand);
                                    }
                                }

                                tx.Complete();
                                result.Success = true;
                                #endregion
                                break;
                        }
                    }
                    else
                    {
                        result.Message = "Quality sampling record is not found.";
                    }
                }
                catch (Exception ex)
                {
                    logger.Error(ex);
                    result.Message = ex.Message;
                }
            }

            return result;
        }

        public async Task<StandardResult> ApplyToProductionTransactions(string QualitySamplingId, 
            List<string> TransactionIds)
        {
            var result = new StandardResult();

            var dc = userContext.GetDataContext();
            var db = dc.Database;
            using(var tx = db.GetTransaction())
            {
                try
                {
                    var qs = await db.FirstOrDefaultAsync<quality_sampling>("WHERE id = @0", QualitySamplingId);
                    logger.Debug(db.LastCommand);

                    if(qs != null)
                    {
                        foreach (var id in TransactionIds)
                        {
                            var record = await db.FirstOrDefaultAsync<production_transaction>("WHERE id = @0", id);
                            logger.Debug(db.LastCommand);
                            if (record != null)
                            {
                                record.quality_sampling_id = qs.id;
                                record.modified_by = userContext.AppUserId;
                                record.modified_on = DateTime.Now;

                                await db.UpdateAsync(record);
                                logger.Debug(db.LastCommand);
                            }
                        }

                        tx.Complete();
                        result.Success = true;
                    }
                    else
                    {
                        result.Message = "Quality sampling record is not found.";
                    }
                }
                catch (Exception ex)
                {
                    logger.Error(ex);
                    result.Message = ex.Message;
                }
            }

            return result;
        }       
    }
}
