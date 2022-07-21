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
    public partial class JointSurvey: ServiceRepository<joint_survey, vw_joint_survey>
    {
        private static readonly Logger logger = LogManager.GetCurrentClassLogger();

        private readonly UserContext userContext;

        public JointSurvey(UserContext userContext)
            : base(userContext.GetDataContext())
        {
            this.userContext = userContext;
        }

        public static async Task<StandardResult> UpdateStockState(string ConnectionString, joint_survey TransactionRecord)
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
                        if (TransactionRecord != null && TransactionRecord.join_survey_date != null
                            && TransactionRecord.approved_on != null)
                        {
                            var record = await db.FirstOrDefaultAsync<joint_survey>(
                                "WHERE id = @0", TransactionRecord.id);

                            if (record != null)
                            {
                                #region Stock state

                                // Select last stock state within period
                                var lastStockState = await db.FirstOrDefaultAsync<stock_state>(
                                    " WHERE stock_location_id = @1 "
                                    + " AND transaction_datetime >= @1 "
                                    + " AND transaction_datetime <= @2 "
                                    + " ORDER BY transaction_datetime DESC LIMIT 1 ", 
                                    TransactionRecord.stockpile_location_id, 
                                    TransactionRecord.start_period_date, 
                                    TransactionRecord.end_period_date);
                                if(lastStockState == null)
                                {
                                    lastStockState = new stock_state()
                                    {
                                        created_by = record.modified_by ?? record.created_by,
                                        created_on = DateTime.Now,
                                        is_active = true,
                                        owner_id = record.owner_id,
                                        organization_id = record.organization_id,
                                        transaction_id = Guid.NewGuid().ToString("N").ToLower(),
                                        transaction_datetime = TransactionRecord.end_period_date,
                                        stock_location_id = TransactionRecord.stockpile_location_id,
                                        joint_survey_id = TransactionRecord.id,
                                        qty_opening = 0,
                                        qty_in = 0,
                                        qty_out = 0,
                                        qty_closing = 0
                                    };
                                }
                                logger.Debug(db.LastCommand);

                                // Select first stock state within period
                                var firstStockState = await db.FirstOrDefaultAsync<stock_state>(
                                    " WHERE stock_location_id = @1 "
                                    + " AND transaction_datetime >= @1 "
                                    + " AND transaction_datetime <= @2 "
                                    + " AND qty_survey IS NOT NULL "
                                    + " ORDER BY transaction_datetime DESC LIMIT 1 ",
                                    TransactionRecord.stockpile_location_id, 
                                    TransactionRecord.start_period_date, 
                                    TransactionRecord.end_period_date);
                                if(firstStockState == null)
                                {
                                    firstStockState = await db.FirstOrDefaultAsync<stock_state>(
                                        " WHERE stock_location_id = @1 "
                                        + " AND transaction_datetime >= @1 "
                                        + " AND transaction_datetime <= @2 "
                                        + " ORDER BY transaction_datetime ASC LIMIT 1 ",
                                    TransactionRecord.stockpile_location_id, 
                                    TransactionRecord.start_period_date, 
                                    TransactionRecord.end_period_date);
                                    if (firstStockState == null)
                                    {
                                        firstStockState = await db.FirstOrDefaultAsync<stock_state>(
                                            " WHERE stock_location_id = @1 "
                                            + " AND transaction_datetime < @1 "
                                            + " ORDER BY transaction_datetime DESC LIMIT 1 ",
                                        TransactionRecord.stockpile_location_id, 
                                        TransactionRecord.start_period_date);
                                        if (firstStockState == null)
                                        {
                                            firstStockState = new stock_state()
                                            {
                                                created_by = record.modified_by ?? record.created_by,
                                                created_on = DateTime.Now,
                                                is_active = true,
                                                owner_id = record.owner_id,
                                                organization_id = record.organization_id,
                                                transaction_id = Guid.NewGuid().ToString("N").ToLower(),
                                                transaction_datetime = TransactionRecord.start_period_date,
                                                stock_location_id = TransactionRecord.stockpile_location_id,
                                                joint_survey_id = TransactionRecord.id,
                                                qty_opening = 0,
                                                qty_in = 0,
                                                qty_out = 0,
                                                qty_closing = 0
                                            };
                                        }
                                    }
                                }
                                logger.Debug(db.LastCommand);

                                // Select all stock states between first and last
                                var stockStates = await db.FetchAsync<stock_state>(
                                    " WHERE stock_location_id = @1 "
                                    + " AND transaction_datetime > @1 "
                                    + " AND transaction_datetime <= @2 ",
                                    TransactionRecord.stockpile_location_id, 
                                    firstStockState.transaction_datetime, 
                                    lastStockState.transaction_datetime);
                                if(stockStates != null)
                                {
                                    if(stockStates.Count > 0)
                                    {
                                        var adjQty = (TransactionRecord.quantity ?? 0 - lastStockState.qty_closing ?? 0) / stockStates.Count;
                                        foreach (var stockState in stockStates.OrderBy(o => o.transaction_datetime))
                                        {
                                            stockState.joint_survey_id = TransactionRecord.id;
                                            stockState.qty_adjustment = adjQty;
                                            stockState.qty_closing = (stockState.qty_opening ?? 0)
                                                + (stockState.qty_in ?? 0)
                                                - (stockState.qty_out ?? 0)
                                                + (stockState.qty_adjustment ?? 0);
                                            await db.UpdateAsync(stockState);
                                        }

                                        if (string.IsNullOrEmpty(lastStockState.id))
                                        {
                                            lastStockState.joint_survey_id = TransactionRecord.id;
                                            lastStockState.qty_opening = stockStates[stockStates.Count - 1].qty_closing;
                                            lastStockState.qty_in = 0;
                                            lastStockState.qty_out = 0;
                                            lastStockState.qty_closing = (lastStockState.qty_opening ?? 0)
                                                + (lastStockState.qty_in ?? 0)
                                                - (lastStockState.qty_out ?? 0);
                                            lastStockState.id = Guid.NewGuid().ToString("N").ToLower();
                                            await db.InsertAsync(lastStockState);
                                        }
                                    }
                                }
                                logger.Debug(db.LastCommand);

                                #endregion
                            }
                            else // Handle deleted transaction
                            {
                                var prevStockState = await db.FirstOrDefaultAsync<stock_state>(
                                    " WHERE stock_location_id = @0 "
                                    + " AND joint_survey_id = @1 "
                                    + " ORDER BY transaction_datetime ",
                                    TransactionRecord.stockpile_location_id,
                                    TransactionRecord.id);

                                var sql = Sql.Builder.Append("UPDATE stock_state");
                                sql.Append("SET joint_survey_id = NULL");
                                sql.Append(", qty_adjustment = NULL");
                                sql.Append(", qty_closing = COALESCE(qty_opening, 0) + COALESCE(qty_in, 0) - COALESCE(qty_out, 0)");
                                sql.Append("WHERE stock_location_id = @0", TransactionRecord.stockpile_location_id);
                                sql.Append("AND joint_survey_id = @0", TransactionRecord.id);
                                await db.ExecuteAsync(sql);
                                logger.Debug(db.LastCommand);

                                var stockStates = await db.FetchAsync<stock_state>(
                                    " WHERE stock_location_id = @0 "
                                    + " AND transaction_datetime > @2 ",
                                    TransactionRecord.stockpile_location_id,
                                    prevStockState.transaction_datetime);
                                if(stockStates != null && stockStates.Count > 0)
                                {
                                    var qty_opening = prevStockState.qty_closing ?? 0;
                                    foreach (var stockState in stockStates.OrderBy(o => o.transaction_datetime))
                                    {
                                        stockState.qty_opening = qty_opening;
                                        stockState.qty_closing = (stockState.qty_opening ?? 0)
                                            + (stockState.qty_in ?? 0)
                                            - (stockState.qty_out ?? 0)
                                            + (stockState.qty_adjustment ?? 0);
                                        if(stockState.qty_survey != null)
                                        {
                                            stockState.qty_in = 0;
                                            stockState.qty_out = 0;
                                            stockState.qty_closing = stockState.qty_survey;
                                            stockState.qty_adjustment = stockState.qty_closing - stockState.qty_opening;
                                        }

                                        await db.UpdateAsync(stockState);
                                        qty_opening = stockState.qty_closing ?? 0;
                                    }
                                }
                                logger.Debug(db.LastCommand);
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

        public async Task<StandardResult> Approve(string JointSurveyId)
        {
            var result = new StandardResult();

            var dc = userContext.GetDataContext();
            var db = dc.Database;
            using (var tx = db.GetTransaction())
            {
                try
                {
                    var record = await db.FirstOrDefaultAsync<joint_survey>("WHERE id = @0", JointSurveyId);
                    logger.Debug(db.LastCommand);

                    if(record != null)
                    {
                        record.approved_by = userContext.AppUserId;
                        record.approved_on = DateTime.Now;
                        await db.UpdateAsync(record);
                        result.Success = true;
                    }
                    else
                    {
                        result.Message = $"Survey {JointSurveyId} is not found";
                    }
                }
                catch (Exception ex)
                {
                    logger.Debug(db.LastCommand);
                    logger.Error(ex.ToString());
                    result.Message = ex.InnerException?.Message ?? ex.Message;
                }
            }

            return result;
        }
    }
}
