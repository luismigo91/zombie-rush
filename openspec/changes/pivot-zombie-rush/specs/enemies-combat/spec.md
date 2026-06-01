## ADDED Requirements

### Requirement: Hordas de zombies en el recorrido

Los zombies SHALL aparecer dentro del recorrido del nivel y avanzar hacia el escuadrón. El sistema MUST soportar varios tipos de enemigo con estadísticas distintas (p. ej. normal, corredor, tanque).

#### Scenario: Avance de la horda

- **WHEN** un zombie aparece en el recorrido
- **THEN** se mueve hacia el escuadrón y puede ser destruido por los proyectiles antes de alcanzarlo

#### Scenario: Tipos con comportamiento distinto

- **WHEN** la horda incluye varios tipos
- **THEN** cada tipo se diferencia al menos en vida y velocidad (p. ej. el corredor llega antes, el tanque aguanta más fuego)

### Requirement: Contacto 1:1

Cuando un zombie alcanza el frente del escuadrón SHALL eliminar exactamente una unidad y, acto seguido, el propio zombie MUST morir.

#### Scenario: Un zombie llega a la fila

- **WHEN** un zombie toca el frente del escuadrón
- **THEN** se descuenta 1 unidad del escuadrón y el zombie es destruido

#### Scenario: El combate es una carrera de fuego

- **WHEN** la cadencia y la cobertura del escuadrón no bastan para frenar a la horda
- **THEN** los zombies van alcanzando el frente y el escuadrón pierde unidades de una en una

### Requirement: Escudo de la fila delantera

Las unidades situadas en el frente del blob SHALL absorber el contacto de los zombies antes que las de detrás, protegiendo temporalmente al resto de la formación.

#### Scenario: Las bajas empiezan por delante

- **WHEN** un zombie impacta en el frente
- **THEN** se pierde una unidad delantera y las unidades traseras permanecen hasta que el frente se agota

### Requirement: Clímax de nivel

Cada nivel SHALL terminar con un clímax de mayor presión (jefe o horda final) que pone a prueba el tamaño alcanzado por el escuadrón.

#### Scenario: Enfrentar el clímax

- **WHEN** el escuadrón llega al final del recorrido
- **THEN** aparece un jefe o una horda final cuya superación determina la victoria del nivel
