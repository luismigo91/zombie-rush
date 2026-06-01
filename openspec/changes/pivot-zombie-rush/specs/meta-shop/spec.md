## ADDED Requirements

### Requirement: Punto de partida permanente

La meta-tienda SHALL definir el punto de partida con el que el escuadrón arranca cada nivel: al menos el número de unidades iniciales y el arma base. Estas compras MUST persistir entre partidas.

#### Scenario: Mejorar las unidades iniciales

- **WHEN** el jugador compra una mejora de unidades iniciales
- **THEN** los siguientes niveles comienzan con un escuadrón mayor

#### Scenario: Persistencia entre sesiones

- **WHEN** el jugador cierra y vuelve a abrir el juego
- **THEN** se conservan las compras de punto de partida realizadas

### Requirement: La tienda no ofrece boosts de porcentaje

La meta-tienda SHALL NO ofrecer las mejoras de estadística por porcentaje del juego anterior (daño, cadencia, vida, velocidad como % escalables por nivel). El crecimiento de poder durante la partida proviene de los elementos del recorrido, no de boosts comprados.

#### Scenario: Catálogo reorientado

- **WHEN** el jugador abre la tienda
- **THEN** ve compras de punto de partida (unidades iniciales, arma base, rendimiento de gates) y no mejoras de % por estadística

### Requirement: Economía alimentada por las partidas

Las partidas SHALL otorgar moneda persistente con la que comprar mejoras de punto de partida en la tienda.

#### Scenario: Ganar y gastar moneda

- **WHEN** el jugador completa o pierde una partida que generó moneda
- **THEN** la moneda se ingresa en un banco persistente y puede gastarse en la tienda
