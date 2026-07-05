using AutoMapper;
using SistemaGastos.Application.DTOs;
using SistemaGastos.Domain.Enums;
using SistemaGastos.Domain.Models;

namespace SistemaGastos.Application.Mappings;

public class MappingProfile : Profile
{
    public MappingProfile()
    {
        CreateMap<CreditCardTransaction, CreditCardTransactionDto>()
            .ForMember(dest => dest.CategoryName, opt => opt.MapFrom(src => src.Category.Name))
            .ForMember(dest => dest.AccountName, opt => opt.MapFrom(src => src.Account.Name))
            .ForMember(dest => dest.Currency, opt => opt.MapFrom(src => src.Account.Currency))
            .ForMember(dest => dest.SharedWith, opt => opt.Ignore());

        CreateMap<CreditCardTransactionDto, CreditCardTransaction>()
            .ForMember(dest => dest.SharedWith, opt => opt.Ignore());

        CreateMap<UpdateCreditCardTransactionDto, CreditCardTransaction>()
            .ForMember(dest => dest.SharedWith, opt => opt.Ignore());

        // === MAPEOS DE CUENTAS (ACCOUNTS) ===

        // 1. Lectura: Entity -> DTO
        // Le decimos explícitamente que convierta el entero de la BD al Enum AccountType
        CreateMap<Account, AccountDto>()
            .ForMember(dest => dest.Type, opt => opt.MapFrom(src => (AccountType)src.Type));

        // 2. Creación: CreateDTO -> Entity
        CreateMap<CreateAccountDto, Account>()
            .ForMember(dest => dest.Type, opt => opt.MapFrom(src => (int)src.Type));

        // 3. Edición: UpdateDTO -> Entity
        CreateMap<UpdateAccountDto, Account>()
            .ForMember(dest => dest.Type, opt => opt.MapFrom(src => (int)src.Type));

        // 4. Dropdowns (Para Transferencias, etc)
        // Usamos la misma lógica para que no explote
        CreateMap<Account, AccountDropdownDto>()
            .ForMember(d => d.Type, opt => opt.MapFrom(s => s.Type.ToString()));

        // (Podés borrar el CreateMap<Account, AccountLookupDto> si no lo estás usando, o agregarlo si tu DTO se llama así)
        CreateMap<Account, AccountLookupDto>()
            .ForMember(dest => dest.Id, opt => opt.MapFrom(src => src.ID))
            .ForMember(dest => dest.DisplayName, opt => opt.MapFrom(src => src.Name));

        CreateMap<Transaction, TransactionDTO>()
            .ForMember(d => d.CategoryType, opt => opt.MapFrom(s => s.Category != null ? s.Category.Type : string.Empty))
            .ForMember(d => d.IsIncome, opt => opt.MapFrom(s => s.Category != null && s.Category.Type == "Ingreso"))
            .ForMember(d => d.AccountName, opt => opt.MapFrom(s => s.Account != null ? s.Account.Name : string.Empty))
            .ForMember(d => d.AccountCurrency, opt => opt.MapFrom(s => s.Account != null ? s.Account.Currency : string.Empty))
            .ForMember(d => d.CategoryName, opt => opt.MapFrom(s => s.Category != null ? s.Category.Name : string.Empty))
            .ForMember(d => d.FixedExpenseName, opt => opt.MapFrom(s => s.FixedExpense != null ? s.FixedExpense.Name : null))
            .ForMember(d => d.AmountFormatted, opt => opt.Ignore())
            .ForMember(d => d.DateFormatted, opt => opt.Ignore());

        CreateMap<FixedExpense, FixedExpenseDto>()
            .ForMember(d => d.AccountName, opt => opt.MapFrom(s => s.Account != null ? s.Account.Name : string.Empty))
            .ForMember(d => d.CategoryName, opt => opt.MapFrom(s => s.Category != null ? s.Category.Name : string.Empty))
            .ForMember(d => d.AmountFormatted, opt => opt.MapFrom(s => s.Amount.ToString("C", new System.Globalization.CultureInfo("es-AR"))))
            .ForMember(d => d.PersonName, opt => opt.MapFrom(s => s.Person != null ? s.Person.Name : null));

        CreateMap<TmpTransaction, TmpTransactionDto>()
            .ForMember(d => d.CategoryName, opt => opt.MapFrom(s => s.Category != null ? s.Category.Name : string.Empty))
            .ForMember(d => d.CategoryType, opt => opt.MapFrom(s => s.Category != null ? s.Category.Type : string.Empty))
            .ForMember(d => d.AccountName, opt => opt.MapFrom(s => s.Account != null ? s.Account.Name : string.Empty))
            .ForMember(d => d.IsIngreso, opt => opt.MapFrom(s => s.Category != null && s.Category.Type == "Ingreso"))
            .ForMember(d => d.AmountFormatted, opt => opt.Ignore());

        // Account mappings
        CreateMap<Account, AccountDropdownDto>()
            .ForMember(d => d.Type, opt => opt.MapFrom(s => s.Type.ToString()));

        // Category mappings
        CreateMap<Category, CategoryDropdownDto>();
    }
}