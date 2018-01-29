﻿// <copyright>
// Copyright by the Spark Development Network
//
// Licensed under the Rock Community License (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
//
// http://www.rockrms.com/license
//
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.
// </copyright>
//
using System;
using System.Linq;

namespace Rock.Model
{
    /// <summary>
    /// Service/Data access class for <see cref="Rock.Model.FinancialTransaction"/> entity objects.
    /// </summary>
    public partial class FinancialTransactionService 
    {
        /// <summary>
        /// Gets a transaction by its transaction code.
        /// </summary>
        /// <param name="transactionCode">The transaction code.</param>
        /// <returns></returns>
        [Obsolete( "Use GetByTransactionCode(financialGatewayId, transaction). This one could return incorrect results if transactions from different financial gateways happen to use the same transaction code") ]
        public FinancialTransaction GetByTransactionCode( string transactionCode )
        {
            return this.GetByTransactionCode( null, transactionCode );
        }

        /// <summary>
        /// Gets a transaction by its transaction code.
        /// </summary>
        /// <param name="financialGatewayId">The financial gateway identifier.</param>
        /// <param name="transactionCode">A <see cref="System.String" /> representing the transaction code for the transaction</param>
        /// <returns>
        /// The <see cref="Rock.Model.FinancialTransaction" /> that matches the transaction code, this value will be null if a match is not found.
        /// </returns>
        public FinancialTransaction GetByTransactionCode( int? financialGatewayId, string transactionCode )
        {
            if ( !string.IsNullOrWhiteSpace( transactionCode ) )
            {
                var qry = Queryable()
                    .Where( t => t.TransactionCode.Equals( transactionCode.Trim(), StringComparison.OrdinalIgnoreCase ) );

                if ( financialGatewayId.HasValue )
                {
                    qry = qry.Where( t => t.FinancialGatewayId.HasValue && t.FinancialGatewayId.Value == financialGatewayId.Value );
                }

                return qry.FirstOrDefault();
            }
            return null;
        }

        /// <summary>
        /// Deletes the specified item.
        /// </summary>
        /// <param name="item">The item.</param>
        /// <returns></returns>
        public override bool Delete( FinancialTransaction item )
        {
            if ( item.FinancialPaymentDetailId.HasValue )
            {
                var paymentDetailsService = new FinancialPaymentDetailService( (Rock.Data.RockContext)this.Context );
                var paymentDetail = paymentDetailsService.Get( item.FinancialPaymentDetailId.Value );
                if ( paymentDetail != null )
                {
                    paymentDetailsService.Delete( paymentDetail );
                }
            }

            return base.Delete( item );
        }
    }
}