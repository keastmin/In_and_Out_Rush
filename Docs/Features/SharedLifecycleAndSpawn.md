# Shared Lifecycle and Spawn

Status: Legacy shared infrastructure

Last reviewed: 2026-08-14

## 책임

Local·Network Entity, System, Visible, View의 공통 수명과 일반 Spawn 정책·샘플링 기반을 제공한다.

## 주요 진입점

- `Assets/02_Scripts/Lifecycle/`
- `Assets/02_Scripts/Spawn/`
- `NetworkSystemBase`, `SystemBase`
- `Assets/02_Scripts/Global/`

## 사용 규칙

기존 기능의 기반으로 유지한다. 새 기능이 이 계층에 새 책임을 추가하기 전에 도메인 내부에 둘 수 있는지 확인한다. `System`처럼 BCL과 충돌하는 이름과 Global Store의 확장은 신중하게 다룬다.

## 관련 기능

Stage, Resource, Territory, Track, Monster 등 다수의 Legacy 시스템.

## 변경 시 확인

- Awake·Spawned·Dispose 호출 순서
- 중복 Initialize·SetUp 방지
- NetworkBehaviour 수명과 Scene unload
- 변경이 모든 소비 기능에 미치는 영향

## 기술 부채

Local·Network 기반 타입과 전역 Store가 넓게 공유된다. 공용 계층의 대규모 정리는 별도 승인 작업으로만 수행한다.
