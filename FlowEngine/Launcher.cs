using Autofac;
using AutoMapper;
using FlowEngine.DAL;
using FlowEngine.DTO;
using FlowEngine.Model;
using Infrastructure.DB;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FlowEngine
{
    public class Launcher
    {
        public static IContainer Container = null;
        public static void InitAutoFac()
        {
            var builder = new ContainerBuilder();
            // Mapper
            var mapper = InitMapper();
            builder.RegisterInstance<IMapper>(mapper).SingleInstance();
            // DB
            builder.RegisterType<OracleHelper>().Named<SqlHelper>("ORACLE").As<SqlHelper>().SingleInstance();
            // DAL
            builder.RegisterType<DAL.LinkDAL>().SingleInstance();
            builder.RegisterType<DAL.NodeDAL>().SingleInstance();
            builder.RegisterType<DAL.PointDAL>().SingleInstance();
            builder.RegisterType<DAL.PropertyDAL>().SingleInstance();
            // Service
            builder.RegisterType<FlowConfigService>().SingleInstance();
            builder.RegisterType<UnitTestRuntimeService>().InstancePerDependency();
            Container = builder.Build();

        }

        public static void InitDatabase()
        {
            // 打开数据库
            Container.ResolveNamed<SqlHelper>("ORACLE").Connect(new COracleParameter().ConnectionString);
        }

        public static IMapper InitMapper()
        {
            var config = new MapperConfiguration(cfg =>
            {
                // Node
                cfg.CreateMap<NodeDTO, NodeViewModel>();
                cfg.CreateMap<NodeViewModel, NodeDTO>();

                // Point
                cfg.CreateMap<PointDTO, ConditionModel>();
                cfg.CreateMap<ConditionModel, PointDTO>();

                // Link
                cfg.CreateMap<LinkDTO, LinkViewModel>();
                cfg.CreateMap<LinkViewModel, LinkDTO>();

                // Property
                cfg.CreateMap<PropertyDTO, PropertyModel>()
                    .ForMember(dest => dest.IsCustom, opt => opt.MapFrom(src => src.ISCUSTOM == 1))
                    .ForMember(dest => dest.DefaultName, opt => opt.MapFrom(src => string.IsNullOrEmpty(src.DEFAULTNAME) ? src.NAME : src.DEFAULTNAME))
                    .ForMember(dest => dest.DataType, opt => opt.MapFrom(src => (Model.DataType)Enum.Parse(typeof(Model.DataType), src.DATATYPE)))
                    .ForMember(dest => dest.Operation, opt => opt.MapFrom(src => (Model.OperationType)Enum.Parse(typeof(Model.OperationType), src.OPERATION)));
                cfg.CreateMap<PropertyModel, PropertyDTO>()
                    .ForMember(dest => dest.ISCUSTOM, opt => opt.MapFrom(src => src.IsCustom ? 1 : 0))
                    .ForMember(dest => dest.DATATYPE, opt => opt.MapFrom(src => src.DataType.ToString()))
                    .ForMember(dest => dest.OPERATION, opt => opt.MapFrom(src => src.Operation.ToString()));
                cfg.CreateMap<NodeDTO, PropertyModel>()
                     .ForMember(dest => dest.NodeId, opt => opt.MapFrom(src => src.ID))
                     .ForMember(dest => dest.Name, opt => opt.MapFrom(src => "文本"))
                     .ForMember(dest => dest.Value, opt => opt.MapFrom(src => src.TEXT))
                     .ForMember(dest => dest.Operation, opt => opt.MapFrom(src => 0))
                     .ForMember(dest => dest.DataType, opt => opt.MapFrom(src => Model.DataType.STRING))
                     .ForMember(dest => dest.IsCustom, opt => opt.MapFrom(src => false));
                cfg.CreateMap<NodeViewModel, PropertyModel>()
                     .ForMember(dest => dest.NodeId, opt => opt.MapFrom(src => src.Id))
                     .ForMember(dest => dest.Name, opt => opt.MapFrom(src => "文本"))
                     .ForMember(dest => dest.Value, opt => opt.MapFrom(src => src.Text))
                     .ForMember(dest => dest.Operation, opt => opt.MapFrom(src => 0))
                     .ForMember(dest => dest.DataType, opt => opt.MapFrom(src => Model.DataType.STRING))
                     .ForMember(dest => dest.IsCustom, opt => opt.MapFrom(src => false));
            });
            var mapper = config.CreateMapper();
            return mapper;
        }
    }
}
