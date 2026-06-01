## ADDED Requirements

### Requirement: Gates en carriles con elección

El recorrido SHALL presentar gates dispuestos en carriles paralelos por los que el escuadrón puede pasar. El jugador MUST poder elegir el gate alineando el escuadrón con su carril antes de cruzarlo.

#### Scenario: Elegir entre gates

- **WHEN** aparecen dos o más gates en carriles distintos
- **THEN** el escuadrón cruza el gate del carril con el que esté alineado y aplica su efecto

#### Scenario: El mejor gate puede estar en el carril más peligroso

- **WHEN** un gate ventajoso comparte carril con una amenaza (horda u obstáculo)
- **THEN** el jugador debe decidir entre el beneficio del gate y el riesgo del carril

### Requirement: Efectos de gate

Un gate SHALL aplicar un efecto sobre el recuento de unidades al cruzarse. Los efectos MUST incluir, al menos, suma (p. ej. +8), multiplicación (p. ej. ×2) y trampa (reducción, p. ej. −5).

#### Scenario: Gate aditivo

- **WHEN** el escuadrón cruza un gate de tipo suma
- **THEN** se añade al recuento la cantidad indicada

#### Scenario: Gate multiplicador

- **WHEN** el escuadrón cruza un gate de tipo multiplicación
- **THEN** el recuento se multiplica por el factor indicado

#### Scenario: Gate trampa

- **WHEN** el escuadrón cruza un gate de tipo trampa
- **THEN** el recuento se reduce según el valor del gate

### Requirement: Gate de arma

Además de los gates que afectan al recuento, el recorrido SHALL incluir gates que **suben el tier del arma global** del escuadrón durante el nivel. Representan el eje de crecimiento de *calidad* (frente al de *cantidad*).

#### Scenario: Subir el arma

- **WHEN** el escuadrón cruza un gate de arma
- **THEN** el arma activa sube un tier (p. ej. pistola → escopeta) para el resto del nivel

#### Scenario: Elegir entre cantidad y calidad

- **WHEN** un gate de arma comparte el cruce con un gate de cantidad (+/×) en otro carril
- **THEN** el jugador elige entre ensanchar el muro (más unidades) o mejorar cada disparo (mejor arma)

### Requirement: Jaulas de supervivientes

El recorrido SHALL contener jaulas con supervivientes que, al liberarse, se incorporan al escuadrón como unidades nuevas.

#### Scenario: Rescatar supervivientes

- **WHEN** el escuadrón libera una jaula (alcanzándola o destruyéndola a tiros, según diseño)
- **THEN** los supervivientes se suman al recuento de unidades

#### Scenario: Jaula no liberada

- **WHEN** el escuadrón no llega a liberar la jaula antes de dejarla atrás
- **THEN** los supervivientes no se obtienen

### Requirement: Barreras destructibles

El recorrido SHALL incluir barreras con vida que bloquean el paso hasta que el fuego del escuadrón las derriba.

#### Scenario: Derribar una barrera

- **WHEN** el escuadrón concentra su fuego sobre una barrera
- **THEN** la barrera pierde vida y, al agotarse, deja pasar al escuadrón

#### Scenario: Barrera intacta frena el avance

- **WHEN** una barrera con vida está en el camino
- **THEN** funciona como obstáculo hasta ser destruida
