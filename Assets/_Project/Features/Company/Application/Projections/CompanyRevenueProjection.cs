using System;
using CleanTime.Company.Domain;
using CleanTime.Employees.Domain;
using CleanTime.Shared.Kernel;

namespace CleanTime.Company.Application
{
    /// <summary>EmployeeRevenueGenerated -> company lifetime revenue.</summary>
    internal sealed class CompanyRevenueProjection : IDisposable
    {
        private readonly ICompanyRepository _company;
        private readonly IDisposable _subscription;

        public CompanyRevenueProjection(IEventBus events, ICompanyRepository company)
        {
            _company = company;
            _subscription = events.Subscribe<EmployeeRevenueGeneratedEvent>(OnRevenue);
        }

        public void Dispose() => _subscription.Dispose();

        private void OnRevenue(EmployeeRevenueGeneratedEvent evt)
        {
            var company = _company.Get();
            if (company.IsFailure)
                return;

            company.Value.AddRevenue(evt.Revenue);
            _company.Save(company.Value);
        }
    }
}
