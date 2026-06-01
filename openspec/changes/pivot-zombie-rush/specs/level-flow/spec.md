## ADDED Requirements

### Requirement: Scroll vertical del mundo

El mundo SHALL desplazarse verticalmente hacia el jugador a un ritmo controlado, presentando el recorrido del nivel (gates, jaulas, barreras y hordas) de abajo hacia arriba. El jugador NO controla el avance vertical, solo su posición en X.

#### Scenario: El recorrido avanza solo

- **WHEN** el nivel está en curso
- **THEN** el mundo scrollea y los elementos del recorrido se aproximan al escuadrón sin que el jugador los empuje

### Requirement: Estructura de nivel

Cada nivel SHALL seguir la estructura inicio → recorrido → clímax: arranque con el escuadrón en su punto de partida, una sección de recorrido con elementos, y un clímax final (jefe u horda).

#### Scenario: Recorrer un nivel completo

- **WHEN** el jugador inicia un nivel
- **THEN** atraviesa la sección de recorrido y desemboca en el clímax final del nivel

### Requirement: Generación procedural de 100 niveles

El juego SHALL ofrecer 100 niveles generados proceduralmente a partir de una semilla, con una curva de dificultad creciente. La generación MUST ser determinista para una misma semilla (mismo nivel → misma disposición).

#### Scenario: Dificultad creciente

- **WHEN** el jugador avanza de nivel
- **THEN** la presión aumenta (más/peores hordas, gates más exigentes) siguiendo la curva de dificultad

#### Scenario: Generación determinista

- **WHEN** se genera el mismo número de nivel con la misma semilla
- **THEN** la disposición de gates, jaulas, barreras y hordas es la misma

### Requirement: Condición de victoria

Superar el clímax de un nivel SHALL contar como victoria y dar acceso al siguiente nivel, hasta completar los 100.

#### Scenario: Completar un nivel

- **WHEN** el escuadrón supera el clímax del nivel
- **THEN** el nivel se marca como superado y se habilita el siguiente

### Requirement: Condición de derrota

Si el recuento de unidades del escuadrón llega a 0 SHALL declararse la derrota del nivel.

#### Scenario: Perder todas las unidades

- **WHEN** el escuadrón se queda sin unidades
- **THEN** el nivel termina en derrota y se ofrece reintentar
