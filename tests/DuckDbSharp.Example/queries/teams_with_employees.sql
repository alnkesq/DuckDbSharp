select
    1 as TeamId,
    [
        { EmployeeId: 6::long, FirstName: 'Susan', LastName: 'Smith' },
        { EmployeeId: 7::long, FirstName: 'John', LastName: 'Doe' },
    ] as Members
union
select
    2 as TeamId,
    [] as Members