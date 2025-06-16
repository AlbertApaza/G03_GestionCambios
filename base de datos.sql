create database BD_GestionDeCambios
create table tbMetodologias(
	idMetodologia int primary key,
	nombre nvarchar(20)
	);
create table tbUsuarios(
	idUsuario int primary key identity,
	usuario nvarchar(50) not null unique, --No se puede repetir usuarios
	contrasena nvarchar(50) not null,
	nombre nvarchar(50),
	apellido nvarchar(50),
	email nvarchar(50) unique,
	fechaCreacion date default cast(getdate() as date),
	estado int not null default 1 
	);

-- Tabla de todos los ciclos de todas las metodologias, ejemplo Incepction de Rup o Sprint 1 de Scrum
create table tbCiclos(
	codCiclo nvarchar(20) primary key,
	nombre nvarchar(50),
	orden int,
	idMetodologia int foreign key references tbMetodologias(idMetodologia) --Id de la metodologia a la que formará parte el ciclo
	);
create table tbProyectos(
	idProyecto int primary key identity,
	nombre nvarchar(100),
	fechaInicio date not null, --fechan inicio del proyecto completo
	fechaFin date null, --fecha fin del proyecto completo
	idUsuarioCreador int not null foreign key references tbUsuarios(idUsuario),
	idMetodologia int not null foreign key references tbMetodologias(idMetodologia),
	codCicloActual nvarchar(20) null foreign key references tbCiclos(codCiclo), --El ciclo actual en el que se encuentra, puede ser Elaboracion o Sprint 3
	estado int not null default 1 --Para ocultar proyectos ponerlo en 0
	);

--Cada ciclo de cada proyecto se le podrá especificar su inicio y fin, para el rup siempre será obligatorio pero para los demas no.
create table tbProyectoCiclo(
	idProyectoCiclo int primary key identity,
	idProyecto int foreign key references tbProyectos(idProyecto),
	codCiclo nvarchar(20) foreign key references tbCiclos(codCiclo),
	inicioCiclo date null,
	finCiclo date null
	);

	
--ELEMENTOS DIFERENTES POR METODOLOGIA
create table tbElementos(
	idElemento int primary key identity,
	nombre nvarchar(100), --Se pueden repetir, pero serán para diferentes metodologias.
	descripcion nvarchar(max)
	);

create table tbRoles(
	idRol int primary key,
	nombre nvarchar(20),
	idMetodologia int null foreign key references tbMetodologias(idMetodologia) --ROL DIFERENTE POR METODOLOGIA
	);
create table tbProyectoElemento(
	idProyectoElemento int primary key identity,
	idProyecto int foreign key references tbProyectos(idProyecto),
	idElemento int foreign key references tbElementos(idElemento),
	fechaInicio date null, --Fecha del elemento de inicio y fin dentro de la fecha de su Ciclo correspondiente
	fechaFin date null,
	idRol int null foreign key references tbRoles(idRol), --EL ROL QUE SE LE ASIGNARÁ EL ELEMENTO
	estado nvarchar(20) default 'Pendiente' check (estado in ('Pendiente', 'En Proceso', 'Finalizado')),
	codCiclo nvarchar(20) null foreign key references tbCiclos(codCiclo)
	);
create table tbProyectoUsuario(
	idProyectoUsuario int primary key identity,
	idProyecto int foreign key references tbProyectos(idProyecto),
	idUsuario int foreign key references tbUsuarios(idUsuario),
	idRol int foreign key references tbRoles(idRol)
	);


--TAREAS DE CADA ELEMENTO
create table tbTareas(
	idTareas int primary key identity,
	idUsuario int foreign key references tbUsuarios(idUsuario), --El usuario que será asignado a la tarea del elemento
	idProyectoElemento int foreign key references tbProyectoElemento(idProyectoElemento),
	nombre nvarchar(50) not null,
	descripcion nvarchar(max),
	estado nvarchar(20) default 'Pendiente' check (estado in ('Pendiente', 'En Proceso', 'Finalizado'))
	);


create table tbTiposDocumento(
	idTipoDocumento int primary key identity,
	nombre nvarchar(50),
	clave nvarchar(20), -- prefijo, como ejemplo use_case , test_plan, o use_case_model
	codCiclo nvarchar(20) foreign key references tbCiclos(codCiclo)
	);
create table tbDocumentos(
	idDocumento int primary key identity,
	idTipoDocumento int foreign key references tbTiposDocumento(idTipoDocumento),
	nombreArchivo nvarchar(50),
	rutaArchivo nvarchar(150),
	version nvarchar(10),
	estado nvarchar(20) default 'Pendiente' check (estado in ('Pendiente', 'En Proceso', 'Finalizado')),
	fechaSubida datetime default getdate(),
	codCiclo nvarchar(20) foreign key references tbCiclos(codCiclo),
	idProyecto int foreign key references tbProyectos(idProyecto),
	comentarios nvarchar(max) null,
	idUsuarioSubida int foreign key references tbUsuarios(idUsuario)
	);

INSERT INTO tbElementos(nombre) VALUES
-- Fase de Inicio (RUP) / Planeación inicial (Scrum, XP)
('Documento de visión del sistema'),
('Acta de constitución del proyecto'),
('Identificación de interesados clave'),
('Backlog del producto'),
('Historias de usuario'),
('Registro de riesgos'),

-- Fase de Elaboración (RUP) / Diseño inicial (XP)
('Casos de uso clave'),
('Prototipo de interfaz de usuario'),
('Modelo de dominio'),
('Diagrama de arquitectura del sistema'),
('Especificaciones suplementarias'),
('Plan de iteración'),

-- Fase de Construcción (RUP) / Desarrollo iterativo (Scrum, XP)
('Código fuente de componentes funcionales'),
('Casos de prueba unitarios'),
('Modelo de implementación'),
('Documentación técnica de módulos'),
('Scripts de compilación automática'),
('Manual del desarrollador interno'),
('Definición de Hecho (Definition of Done)'),
('Plan de pruebas'),

-- Fase de Transición (RUP) / Cierre de sprint o release (Scrum, XP)
('Versión ejecutable para usuarios finales'),
('Plan de despliegue del sistema'),
('Guía de instalación del producto'),
('Guía del usuario final'),
('Plan de soporte post-entrega'),
('Informe de retroalimentación del cliente');


CREATE TABLE tbSolicitudesCambio (
    idSolicitudCambio INT primary key IDENTITY NOT NULL,
    codigoDocumentoSolicitd NVARCHAR(30) NOT NULL DEFAULT 'R-GCSW001',
	fechaSolicitud DATE,
    idProyecto INT NOT NULL foreign key references tbProyectos(idProyecto),
    idUsuarioSolicitante INT NOT NULL foreign key references tbUsuarios(idUsuario),

    objetivoSolicitud NVARCHAR(MAX) NOT NULL,
    descripcionSolicitud NVARCHAR(MAX) NOT NULL,
	idElementoAfectado int foreign key references tbProyectoElemento(idProyectoElemento),
	impactoEstimado NVARCHAR(MAX) NOT NULL,
	esfuerzoEstimado NVARCHAR(MAX) NOT NULL,


	idUsuarioReceptor INT NULL foreign key references tbUsuarios(idUsuario),
	fechaRecibida DATE null,


	pasoActualProceso int not null default 1,
	fechaInicioImplementacionCambio date null,
	fechaCierreDelCambio date null,
	observaciones NVARCHAR(MAX) null,
	estadoSolicitud nvarchar(20) not null default 'Propuesto' CHECK (estadoSolicitud IN ('Propuesto', 'Aprobado', 'Planificado', 'Implantado', 'Cancelado'))

	)

ALTER TABLE tbSolicitudesCambio
ADD fechaEstado DATETIME NULL
ALTER TABLE tbSolicitudesCambio
ADD GiroJefeProyectoFecha DATETIME NULL
ALTER TABLE tbSolicitudesCambio
ADD ModificacionVersionFecha DATETIME NULL